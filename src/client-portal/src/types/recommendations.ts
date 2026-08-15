export interface RecommendationItem {
  id: string;
  title: string;
  genre: string;
  quality: string;
  score: number;
  description?: string;
  posterUrl?: string;
}

export interface RecommendationsResponse {
  userId: string;
  tier: string;
  source: string;
  generatedAt: string;
  items: RecommendationItem[];
}

export interface DegradedResponse {
  status: string;
  reason: string;
  retryAfterSeconds: number;
  message?: string;
}

export interface HomepageHeroBanner {
  id: string;
  title: string;
  subtitle: string;
  backgroundImageUrl: string;
  actionUrl: string;
  badge: string;
}

export interface HomepageRow {
  rowId: string;
  title: string;
  category: string;
  items: RecommendationItem[];
}

export interface HomepageLayoutResponse {
  userId: string;
  layoutType: string;
  source: string;
  generatedAt: string;
  hero: HomepageHeroBanner;
  rows: HomepageRow[];
}

export type CircuitBreakerState = 'CLOSED' | 'OPEN' | 'HALF_OPEN';
export type CacheStatus = 'HIT' | 'MISS' | 'FALLBACK_STATIC' | 'PRECOMPUTED_PRIMARY';
