import { RecommendationItem, HomepageHeroBanner } from '../types/recommendations';

export const STATIC_POPULAR_FALLBACKS: RecommendationItem[] = [
  {
    id: 'fallback-001',
    title: 'Dune: The Prophecy',
    genre: 'Sci-Fi / Epic',
    quality: '4K Ultra HD',
    score: 0.97,
    description: 'Set 10,000 years before the ascension of Paul Atreides, two Harkonnen sisters combat forces threatening humankind.',
    posterUrl: 'https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=600&q=80'
  },
  {
    id: 'fallback-002',
    title: 'The Dark Knight: Legacy',
    genre: 'Action / Crime',
    quality: '4K Dolby Vision',
    score: 0.98,
    description: 'When the menace known as the Joker wreaks havoc and chaos on the people of Gotham, Batman must accept one of the greatest tests.',
    posterUrl: 'https://images.unsplash.com/photo-1509198397868-475647b2a1e5?auto=format&fit=crop&w=600&q=80'
  },
  {
    id: 'fallback-003',
    title: 'Blade Runner 2049: Replicant Dawn',
    genre: 'Sci-Fi / Noir',
    quality: '4K HDR10+',
    score: 0.95,
    description: 'Young Blade Runner K unearths a long-buried secret that leads him to track down former Blade Runner Rick Deckard.',
    posterUrl: 'https://images.unsplash.com/photo-1578632767115-351597cf2477?auto=format&fit=crop&w=600&q=80'
  },
  {
    id: 'fallback-004',
    title: 'Planet Earth: Frozen Frontiers',
    genre: 'Documentary / Nature',
    quality: '4K 60FPS',
    score: 0.99,
    description: 'Experience the ultimate journey across Earth’s icy poles, capturing animal behaviors never before seen on film.',
    posterUrl: 'https://images.unsplash.com/photo-1464822759023-fed622ff2c3b?auto=format&fit=crop&w=600&q=80'
  },
  {
    id: 'fallback-005',
    title: 'Inception: Dream State',
    genre: 'Sci-Fi / Thriller',
    quality: '4K Dolby Atmos',
    score: 0.96,
    description: 'A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea.',
    posterUrl: 'https://images.unsplash.com/photo-1518709268805-4e9042af9f23?auto=format&fit=crop&w=600&q=80'
  }
];

export const STATIC_FALLBACK_HERO: HomepageHeroBanner = {
  id: 'hero-fallback-static',
  title: 'Top Rated Worldwide: Popular Picks',
  subtitle: 'Serving critically acclaimed blockbusters and timeless classics while live recommendation services synchronize.',
  backgroundImageUrl: 'https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80',
  actionUrl: '/watch/fallback-001',
  badge: 'Offline Static Resilience Fallback'
};
