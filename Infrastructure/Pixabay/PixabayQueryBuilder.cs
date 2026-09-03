namespace NTWallpaper.Infrastructure.Pixabay;

using NTWallpaper.Domain.Models;
using NTWallpaper.Domain.Options;

/// <summary>Builds a validated Pixabay request URL from <see cref="ImageSearchOptions"/>.</summary>
public static class PixabayQueryBuilder
{
    public static string Build(string baseUrl, string apiKey, ImageSearchOptions o)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new PixabayException("Pixabay API key is not configured.");

        var query = (o.Query ?? string.Empty).Trim();
        if (query.Length > 100)
            query = query[..100];

        var parameters = new Dictionary<string, string?>
        {
            ["key"] = apiKey,
            ["q"] = query,
            ["image_type"] = o.ImageType.ToApiString(),
            ["orientation"] = o.Orientation.ToApiString(),
            ["safesearch"] = o.SafeSearch ? "true" : "false",
            ["order"] = string.IsNullOrWhiteSpace(o.Order) ? "popular" : o.Order,
            ["page"] = Math.Max(1, o.Page).ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["per_page"] = Math.Clamp(o.PerPage, 3, 200).ToString(System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(o.Lang))
            parameters["lang"] = o.Lang;
        if (!string.IsNullOrWhiteSpace(o.Category))
            parameters["category"] = o.Category;
        if (o.MinWidth.HasValue && o.MinWidth.Value > 0)
            parameters["min_width"] = o.MinWidth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (o.MinHeight.HasValue && o.MinHeight.Value > 0)
            parameters["min_height"] = o.MinHeight.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(o.Colors))
            parameters["colors"] = o.Colors;
        if (o.EditorsChoice)
            parameters["editors_choice"] = "true";

        var encoded = string.Join("&",
            parameters
                .Where(kv => kv.Value is not null)
                .Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value!)}"));

        return $"{baseUrl.TrimEnd('/')}/?{encoded}";
    }

    /// <summary>Builds a URL safe for logging (API key omitted).</summary>
    public static string BuildForLog(string baseUrl, ImageSearchOptions o)
    {
        var copy = new ImageSearchOptions
        {
            Query = o.Query,
            Lang = o.Lang,
            Category = o.Category,
            ImageType = o.ImageType,
            Orientation = o.Orientation,
            MinWidth = o.MinWidth,
            MinHeight = o.MinHeight,
            Colors = o.Colors,
            EditorsChoice = o.EditorsChoice,
            SafeSearch = o.SafeSearch,
            Order = o.Order,
            Page = o.Page,
            PerPage = o.PerPage
        };
        return Build(baseUrl, "***REDACTED***", copy);
    }
}
