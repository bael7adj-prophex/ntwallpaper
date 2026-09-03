namespace NTWallpaper.Application.Services;

using NTWallpaper.Domain.Interfaces;
using NTWallpaper.Domain.Models;

/// <summary>
/// Deterministic, configurable scoring engine that picks the best candidate image
/// for a group's targets. Weights come from <see cref="AppSettings"/>.
/// </summary>
public class RecommendationService : IRecommendationService
{
    public ImageResult? SelectBest(
        IReadOnlyList<ImageResult> candidates,
        IReadOnlyList<WallpaperTarget> targets,
        ISet<long> recentlyUsed,
        AppSettings settings)
    {
        if (candidates.Count == 0 || targets.Count == 0)
            return candidates.Count > 0 ? candidates[0] : null;

        var avgW = targets.Average(t => t.Width);
        var avgH = targets.Average(t => t.Height);
        var targetAspect = avgW / Math.Max(avgH, 1.0);
        var maxDim = targets.Max(t => Math.Max(t.Width, t.Height));

        ImageResult? best = null;
        var bestScore = double.NegativeInfinity;

        foreach (var c in candidates)
        {
            var score = Score(c, avgW, avgH, targetAspect, maxDim, recentlyUsed, settings);
            if (score > bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        return best;
    }

    private static double Score(
        ImageResult c,
        double avgW,
        double avgH,
        double targetAspect,
        double maxDim,
        ISet<long> recentlyUsed,
        AppSettings s)
    {
        // Resolution compatibility: 1.0 if at least target size, otherwise ratio of coverage.
        var resScore = (c.Width >= avgW && c.Height >= avgH)
            ? 1.0
            : Math.Min(c.Width / Math.Max(avgW, 1.0), c.Height / Math.Max(avgH, 1.0));

        // Aspect ratio compatibility (0..1).
        var imgAspect = c.Width / Math.Max(c.Height, 1.0);
        var aspectScore = 1.0 - Math.Min(1.0, Math.Abs(imgAspect - targetAspect) / Math.Max(targetAspect, 0.0001));

        // Image quality proxy: pixel count relative to the largest target dimension squared.
        var qualityScore = Math.Min(1.0, (c.Width * (double)c.Height) / (maxDim * maxDim + 1));

        // Search relevance: Pixabay provides no semantic score, so use a neutral baseline.
        const double relevanceScore = 0.5;

        // Popularity: log-scaled views.
        var popScore = c.Views > 0 ? Math.Min(1.0, Math.Log10(c.Views + 1) / 7.0) : 0.0;

        // Editor's Choice proxy: Pixabay hits do not expose editors_choice, use favorites.
        var ecScore = c.Favorites > 0 ? Math.Min(1.0, Math.Log10(c.Favorites + 1) / 5.0) : 0.0;

        // Novelty: penalize recently used images.
        var noveltyScore = recentlyUsed.Contains(c.PixabayId) ? 0.0 : 1.0;

        return resScore * s.WeightResolution
             + aspectScore * s.WeightAspectRatio
             + qualityScore * s.WeightQuality
             + relevanceScore * s.WeightRelevance
             + popScore * s.WeightPopularity
             + ecScore * s.WeightEditorsChoice
             + noveltyScore * s.WeightNovelty;
    }
}
