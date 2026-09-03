namespace NTWallpaper.Infrastructure.Pixabay;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>Pixabay implementation of <see cref="IImageProvider"/> with retry/backoff.</summary>
public class PixabayImageProvider : IImageProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    private readonly ISettingsService _settings;
    private readonly ILogger<PixabayImageProvider> _logger;
    private const string BaseUrl = "https://pixabay.com/api/";

    public string Name => "Pixabay";

    public PixabayImageProvider(ISettingsService settings, ILogger<PixabayImageProvider> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ImageResult>> SearchAsync(ImageSearchOptions options, CancellationToken cancellationToken)
    {
        var apiKey = await _settings.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new PixabayException("Pixabay API key is not configured.");

        var url = PixabayQueryBuilder.Build(BaseUrl, apiKey, options);
        _logger.LogInformation("Pixabay search: {Query} (per_page={PerPage}, page={Page})", options.Query, options.PerPage, options.Page);

        var response = await RetryAsync(async () =>
        {
            using var responseMessage = await HttpClient.GetAsync(url, cancellationToken);
            if (responseMessage.StatusCode == HttpStatusCode.Unauthorized ||
                responseMessage.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new PixabayException(
                    $"Pixabay rejected the request ({responseMessage.StatusCode}). Verify the API key.", retryable: false);
            }

            if (responseMessage.StatusCode == HttpStatusCode.BadRequest)
            {
                var body = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
                throw new PixabayException($"Pixabay reported a bad request: {body}", retryable: false);
            }

            if ((int)responseMessage.StatusCode == 429)
                throw new PixabayException("Pixabay rate limit reached (HTTP 429).", retryable: true);

            if (!responseMessage.IsSuccessStatusCode)
                throw new PixabayException($"Pixabay returned {(int)responseMessage.StatusCode}.", retryable: true);

            var parsed = await responseMessage.Content.ReadFromJsonAsync<PixabayResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);

            return parsed ?? new PixabayResponse();
        }, _settings.Current.RetryCount, cancellationToken);

        var results = response.Hits.Select(Map).ToList();
        foreach (var r in results)
            r.SearchTerm = options.Query;
        return results;
    }

    private static ImageResult Map(PixabayHit h)
    {
        var orientation = h.ImageWidth >= h.ImageHeight ? "horizontal" : "vertical";
        return new ImageResult
        {
            PixabayId = h.Id,
            SourceUrl = h.LargeImageURL,
            PreviewUrl = h.PreviewURL,
            LargeImageUrl = h.LargeImageURL,
            PageUrl = h.PageURL,
            Width = h.ImageWidth,
            Height = h.ImageHeight,
            ImageType = h.Type,
            Orientation = orientation,
            Views = h.Views,
            Downloads = h.Downloads,
            Likes = h.Likes,
            Comments = h.Comments,
            Favorites = h.Favorites,
            UserId = h.UserId,
            UserName = h.User,
            Tags = h.Tags
        };
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> action, int retries, CancellationToken cancellationToken)
    {
        var attempt = 0;
        while (true)
        {
            try
            {
                return await action();
            }
            catch (PixabayException ex) when (!ex.IsRetryable || attempt >= retries)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < retries)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Pixabay request failed (attempt {Attempt}/{Max}). Retrying in {Delay}s.", attempt, retries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
