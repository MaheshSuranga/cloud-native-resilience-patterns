using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using RecommendationsService.Models;

namespace RecommendationsService.Services;

public class HomepagePrecomputeEngine : IHomepagePrecomputeEngine
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<HomepagePrecomputeEngine> _logger;

    public HomepagePrecomputeEngine(IDistributedCache cache, ILogger<HomepagePrecomputeEngine> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public HomepageLayoutResponse GetGlobalDefaultLayout()
    {
        var defaultHero = new HomepageHeroBanner(
            Id: "hero-global-default",
            Title: "Stream the World's Greatest Stories",
            Subtitle: "Explore thousands of award-winning blockbusters, originals, and documentaries.",
            BackgroundImageUrl: "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80",
            ActionUrl: "/browse/trending",
            Badge: "Universal Featured"
        );

        var trendingRow = new HomepageRow(
            RowId: "row-trending-global",
            Title: "Trending Worldwide",
            Category: "Trending",
            Items: new List<RecommendationItem>
            {
                new("mov-pop-01", "Dune Horizon", "Sci-Fi", "4K HDR", 0.96, "An epic journey across the desert planet.", "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=600&q=80"),
                new("mov-pop-02", "Shadow Protocol", "Action / Spy", "Full HD", 0.93, "An undercover agent hunted by rogue operatives.", "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=600&q=80"),
                new("mov-pop-03", "Apex Predators", "Documentary", "4K Dolby Vision", 0.91, "Survival in the world's most remote ecosystems.", "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=600&q=80")
            }
        );

        return new HomepageLayoutResponse(
            UserId: "global_default",
            LayoutType: "GlobalDefaultFallback",
            Source: "FastFallbackStatic",
            GeneratedAt: DateTimeOffset.UtcNow,
            Hero: defaultHero,
            Rows: new List<HomepageRow> { trendingRow }
        );
    }

    public async Task<HomepageLayoutResponse> ComputeAndCacheLayoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Computing personalized homepage layout for User '{UserId}'...", userId);

        var isPremium = userId.Contains("premium", StringComparison.OrdinalIgnoreCase) ||
                        userId.Contains("4k", StringComparison.OrdinalIgnoreCase) ||
                        (int.TryParse(userId.Replace("user", "", StringComparison.OrdinalIgnoreCase), out var idNum) && idNum % 2 == 0) ||
                        userId.Equals("user123", StringComparison.OrdinalIgnoreCase);

        var hero = isPremium
            ? new HomepageHeroBanner(
                Id: $"hero-{userId}",
                Title: "Cyberpunk 2099: Neon Horizon [4K Director's Cut]",
                Subtitle: "Experience the pulse of Neo-Tokyo in ultra-high fidelity with Dolby Atmos 3D audio.",
                BackgroundImageUrl: "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=1200&q=80",
                ActionUrl: "/watch/mov-4k-002",
                Badge: "Personalized 4K Exclusive"
              )
            : new HomepageHeroBanner(
                Id: $"hero-{userId}",
                Title: "Metro Chronicle: City of Whispers",
                Subtitle: "An investigative reporter unravels high-stakes mystery in the heart of the metropolis.",
                BackgroundImageUrl: "https://images.unsplash.com/photo-1477959858617-67f30bc75b82?auto=format&fit=crop&w=1200&q=80",
                ActionUrl: "/watch/mov-std-001",
                Badge: "Curated For You"
              );

        var continueWatchingRow = new HomepageRow(
            RowId: "row-continue-watching",
            Title: "Continue Watching for You",
            Category: "Personalized",
            Items: new List<RecommendationItem>
            {
                new("rec-cw-01", "Interstellar Odyssey: Part 2", "Sci-Fi", isPremium ? "4K HDR" : "1080p", 0.99, "Continuing episode 4 (45 mins remaining)", "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=600&q=80")
            }
        );

        var topPicksRow = new HomepageRow(
            RowId: "row-top-picks",
            Title: isPremium ? "Top Picks in Pristine 4K UHD" : "Top Picks Recommended for You",
            Category: "AI Recommended",
            Items: new List<RecommendationItem>
            {
                new("rec-tp-01", "Quantum Paradox", "Thriller", isPremium ? "4K Dolby Vision" : "Full HD", 0.95, "Mind-bending physics thriller.", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=600&q=80"),
                new("rec-tp-02", "The Alpine Solitude", "Documentary", isPremium ? "4K UHD 60FPS" : "Full HD", 0.94, "Glacial majesty and pristine mountain peaks.", "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=600&q=80"),
                new("rec-tp-03", "Midnight Velocity", "Action", "Full HD 1080p", 0.88, "Underground racing adrenaline.", "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=600&q=80")
            }
        );

        var layout = new HomepageLayoutResponse(
            UserId: userId,
            LayoutType: "PersonalizedPrecomputed",
            Source: "EVCache-PrecomputeEngine",
            GeneratedAt: DateTimeOffset.UtcNow,
            Hero: hero,
            Rows: new List<HomepageRow> { continueWatchingRow, topPicksRow }
        );

        var cacheKey = $"user:{userId}:homepage:v1";
        try
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24),
                SlidingExpiration = TimeSpan.FromHours(4)
            };
            var serialized = JsonSerializer.Serialize(layout);
            await _cache.SetStringAsync(cacheKey, serialized, cacheOptions, cancellationToken);
            _logger.LogInformation("Successfully written precomputed homepage to Redis key '{CacheKey}'", cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist precomputed layout into Redis for key '{CacheKey}'", cacheKey);
        }

        return layout;
    }

    public async Task<int> BatchPrecomputeAndCacheAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var idList = userIds.Distinct().ToList();
        _logger.LogInformation("Starting high-throughput batch precompute for {Count} users...", idList.Count);

        // Execute batch calculations in parallel utilizing Task.WhenAll
        var tasks = idList.Select(userId => ComputeAndCacheLayoutAsync(userId, cancellationToken));
        await Task.WhenAll(tasks);

        sw.Stop();
        _logger.LogInformation("High-throughput batch precompute completed for {Count} users in {ElapsedMs}ms.", idList.Count, sw.ElapsedMilliseconds);
        return idList.Count;
    }
}
