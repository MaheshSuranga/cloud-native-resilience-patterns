import { useState, useEffect, useCallback } from 'react';
import { HomepageLayoutResponse, CacheStatus } from '../types/recommendations';
import { STATIC_FALLBACK_HERO, STATIC_POPULAR_FALLBACKS } from '../data/staticFallbacks';

export function useHomepageLayout(userId: string) {
  const [layout, setLayout] = useState<HomepageLayoutResponse>({
    userId,
    layoutType: 'Initial',
    source: 'StaticFallback',
    generatedAt: new Date().toISOString(),
    hero: STATIC_FALLBACK_HERO,
    rows: [{ rowId: 'row-static-01', title: 'Popular Picks Worldwide', category: 'Universal', items: STATIC_POPULAR_FALLBACKS }]
  });

  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [cacheStatus, setCacheStatus] = useState<CacheStatus>('PRECOMPUTED_PRIMARY');
  const [latencyMs, setLatencyMs] = useState<number>(0);
  const [isPrecomputingOutofBand, setIsPrecomputingOutofBand] = useState<boolean>(false);

  const fetchLayout = useCallback(async (targetUserId: string = userId) => {
    setIsLoading(true);
    const startTime = performance.now();

    try {
      const response = await fetch(`/homepage/${encodeURIComponent(targetUserId)}`, {
        headers: { 'Accept': 'application/json' }
      });

      const elapsed = Math.round(performance.now() - startTime);
      setLatencyMs(elapsed);

      const storeHeader = response.headers.get('X-Cache-Store');
      if (storeHeader === 'EVCache-Primary') {
        setCacheStatus('PRECOMPUTED_PRIMARY');
        setIsPrecomputingOutofBand(false);
      } else {
        setCacheStatus('FALLBACK_STATIC');
        setIsPrecomputingOutofBand(true);
      }

      if (response.ok) {
        const data: HomepageLayoutResponse = await response.json();
        setLayout(data);
      }
    } catch (err) {
      const elapsed = Math.round(performance.now() - startTime);
      setLatencyMs(elapsed);
      setCacheStatus('FALLBACK_STATIC');
    } finally {
      setIsLoading(false);
    }
  }, [userId]);

  useEffect(() => {
    fetchLayout(userId);
  }, [userId, fetchLayout]);

  return {
    layout,
    isLoading,
    cacheStatus,
    latencyMs,
    isPrecomputingOutofBand,
    refetch: fetchLayout
  };
}
