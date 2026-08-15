import { useState, useEffect, useCallback, useRef } from 'react';
import {
  RecommendationItem,
  RecommendationsResponse,
  CircuitBreakerState,
  CacheStatus
} from '../types/recommendations';
import { STATIC_POPULAR_FALLBACKS } from '../data/staticFallbacks';

interface UseRecommendationsOptions {
  timeoutMs?: number;
}

export function useRecommendations(userId: string, options: UseRecommendationsOptions = {}) {
  const { timeoutMs = 2500 } = options;

  const [items, setItems] = useState<RecommendationItem[]>(STATIC_POPULAR_FALLBACKS);
  const [tier, setTier] = useState<string>('4K Ultra');
  const [source, setSource] = useState<string>('InitialLoad');
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [isDegraded, setIsDegraded] = useState<boolean>(false);
  const [circuitState, setCircuitState] = useState<CircuitBreakerState>('CLOSED');
  const [cacheStatus, setCacheStatus] = useState<CacheStatus>('MISS');
  const [latencyMs, setLatencyMs] = useState<number>(0);
  const [errorState, setErrorState] = useState<string | null>(null);
  const [retryCountdown, setRetryCountdown] = useState<number>(0);

  const countdownTimerRef = useRef<number | null>(null);

  const startCountdown = useCallback((seconds: number) => {
    if (countdownTimerRef.current) {
      clearInterval(countdownTimerRef.current);
    }
    setRetryCountdown(seconds);
    countdownTimerRef.current = window.setInterval(() => {
      setRetryCountdown((prev) => {
        if (prev <= 1) {
          if (countdownTimerRef.current) clearInterval(countdownTimerRef.current);
          setCircuitState('HALF_OPEN');
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  const fetchRecommendations = useCallback(async (targetUserId: string = userId) => {
    setIsLoading(true);
    setErrorState(null);
    const startTime = performance.now();

    const controller = new AbortController();
    const timerId = setTimeout(() => controller.abort(), timeoutMs);

    try {
      const response = await fetch(`/recommendations/${encodeURIComponent(targetUserId)}`, {
        signal: controller.signal,
        headers: {
          'Accept': 'application/json'
        }
      });

      clearTimeout(timerId);
      const elapsed = Math.round(performance.now() - startTime);
      setLatencyMs(elapsed);

      const cacheHeader = response.headers.get('X-Cache');
      if (cacheHeader === 'HIT') {
        setCacheStatus('HIT');
      } else {
        setCacheStatus('MISS');
      }

      if (response.status === 200) {
        const data: RecommendationsResponse = await response.json();
        setItems(data.items);
        setTier(data.tier);
        setSource(data.source);
        setIsDegraded(false);
        setCircuitState('CLOSED');
        setRetryCountdown(0);
      } else if (response.status === 503) {
        // Intercept Circuit Breaker Open
        const degradedData = await response.json().catch(() => null);
        const retrySec = degradedData?.retryAfterSeconds || 15;
        
        setIsDegraded(true);
        setCircuitState('OPEN');
        setCacheStatus('FALLBACK_STATIC');
        setItems(STATIC_POPULAR_FALLBACKS);
        setSource('ResilientFallback-CircuitOpen');
        setErrorState(`Circuit Breaker OPEN: ${degradedData?.message || 'Downstream service unavailable'}`);
        startCountdown(retrySec);
      } else {
        throw new Error(`Unexpected server response: ${response.status} ${response.statusText}`);
      }
    } catch (err: unknown) {
      clearTimeout(timerId);
      const elapsed = Math.round(performance.now() - startTime);
      setLatencyMs(elapsed);

      setIsDegraded(true);
      setCircuitState('OPEN');
      setCacheStatus('FALLBACK_STATIC');
      setItems(STATIC_POPULAR_FALLBACKS);
      setSource('ResilientFallback-TimeoutOrNetwork');

      const isAbort = (err as Error)?.name === 'AbortError';
      const errMsg = isAbort
        ? `Client AbortController Timeout (>${timeoutMs}ms) triggered.`
        : (err as Error)?.message || 'Network communication error';

      setErrorState(errMsg);
      startCountdown(15);
    } finally {
      setIsLoading(false);
    }
  }, [userId, timeoutMs, startCountdown]);

  useEffect(() => {
    fetchRecommendations(userId);
    return () => {
      if (countdownTimerRef.current) clearInterval(countdownTimerRef.current);
    };
  }, [userId, fetchRecommendations]);

  return {
    items,
    tier,
    source,
    isLoading,
    isDegraded,
    circuitState,
    cacheStatus,
    latencyMs,
    errorState,
    retryCountdown,
    refetch: fetchRecommendations
  };
}
