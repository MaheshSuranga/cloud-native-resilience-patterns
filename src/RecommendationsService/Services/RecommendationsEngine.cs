using RecommendationsService.Models;

namespace RecommendationsService.Services;

public class RecommendationsEngine : IRecommendationsEngine
{
    private static readonly IReadOnlyList<RecommendationItem> Premium4KCatalog = new List<RecommendationItem>
    {
        new(
            Id: "mov-4k-001",
            Title: "Interstellar Odyssey: Deep Space",
            Genre: "Sci-Fi / Adventure",
            Quality: "4K Dolby Vision / Atmos",
            Score: 0.98,
            Description: "An expedition beyond the known galaxy in pristine high dynamic range and immersive spatial audio.",
            PosterUrl: "https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=600&q=80"
        ),
        new(
            Id: "mov-4k-002",
            Title: "Cyberpunk 2099: Neon Horizon",
            Genre: "Action / Cyberpunk",
            Quality: "4K HDR10+ / Dolby Atmos",
            Score: 0.95,
            Description: "A rogue synthetic operative battles megacorporations across a sprawling neon dystopia.",
            PosterUrl: "https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=600&q=80"
        ),
        new(
            Id: "mov-4k-003",
            Title: "The Alpine Solitude",
            Genre: "Documentary",
            Quality: "4K UHD 60FPS",
            Score: 0.92,
            Description: "Breathtaking cinematography across the glaciers and peaks of the European Alps.",
            PosterUrl: "https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=600&q=80"
        ),
        new(
            Id: "mov-4k-004",
            Title: "Quantum Paradox",
            Genre: "Thriller / Sci-Fi",
            Quality: "4K Dolby Vision",
            Score: 0.91,
            Description: "When a particle physics experiment fractures reality, a team must navigate parallel dimensions.",
            PosterUrl: "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=600&q=80"
        )
    };

    private static readonly IReadOnlyList<RecommendationItem> StandardCatalog = new List<RecommendationItem>
    {
        new(
            Id: "mov-std-001",
            Title: "Metro Chronicle",
            Genre: "Drama / Mystery",
            Quality: "Full HD 1080p",
            Score: 0.88,
            Description: "An investigative reporter uncovers corruption spanning the city transit authority.",
            PosterUrl: "https://images.unsplash.com/photo-1477959858617-67f30bc75b82?auto=format&fit=crop&w=600&q=80"
        ),
        new(
            Id: "mov-std-002",
            Title: "Midnight Velocity",
            Genre: "Action / Racing",
            Quality: "Full HD 1080p",
            Score: 0.85,
            Description: "Underground street racers risk everything in high-stakes nocturnal competitions.",
            PosterUrl: "https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=600&q=80"
        ),
        new(
            Id: "mov-std-003",
            Title: "Echoes of the Coast",
            Genre: "Documentary",
            Quality: "Full HD 1080p",
            Score: 0.82,
            Description: "Exploring the fragile marine ecosystems along the Pacific rim.",
            PosterUrl: "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?auto=format&fit=crop&w=600&q=80"
        )
    };

    public RecommendationsResponse GenerateRecommendations(UserEntitlementDto entitlement)
    {
        var items = entitlement.Tier.Equals("4K", StringComparison.OrdinalIgnoreCase) || entitlement.IsPremium
            ? Premium4KCatalog
            : StandardCatalog;

        return new RecommendationsResponse(
            UserId: entitlement.UserId,
            Tier: entitlement.Tier,
            Source: "LiveGenerated",
            GeneratedAt: DateTimeOffset.UtcNow,
            Items: items
        );
    }
}
