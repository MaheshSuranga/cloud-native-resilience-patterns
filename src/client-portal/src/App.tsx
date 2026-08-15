import { useState } from 'react';
import { Header } from './components/Header';
import { HeroBanner } from './components/HeroBanner';
import { RecommendationsCarousel } from './components/RecommendationsCarousel';
import { ResilientFallbackBanner } from './components/ResilientFallbackBanner';
import { ChaosControlPanel } from './components/ChaosControlPanel';
import { useRecommendations } from './hooks/useRecommendations';
import { useHomepageLayout } from './hooks/useHomepageLayout';
import { STATIC_FALLBACK_HERO } from './data/staticFallbacks';
import { Activity, ShieldCheck, Database, Layers } from 'lucide-react';

export function App() {
  const [currentUserId, setCurrentUserId] = useState<string>('user123');
  const [activeMode, setActiveMode] = useState<'CacheAside' | 'EVCache'>('CacheAside');

  // Step 1 Hook: Cache-Aside with Polly v8 Resilience Pipeline
  const recHook = useRecommendations(currentUserId);

  // Step 4 Hook: Netflix-Style EVCache Primary Store
  const evHook = useHomepageLayout(currentUserId);

  // Determine current active telemetry
  const isCacheAside = activeMode === 'CacheAside';
  const circuitState = isCacheAside ? recHook.circuitState : 'CLOSED';
  const cacheStatus = isCacheAside ? recHook.cacheStatus : evHook.cacheStatus;
  const latencyMs = isCacheAside ? recHook.latencyMs : evHook.latencyMs;
  const isDegraded = isCacheAside ? recHook.isDegraded : false;
  const isLoading = isCacheAside ? recHook.isLoading : evHook.isLoading;

  const handleRefresh = () => {
    if (isCacheAside) {
      recHook.refetch(currentUserId);
    } else {
      evHook.refetch(currentUserId);
    }
  };

  const activeHero = isCacheAside
    ? (recHook.items.length > 0 && !isDegraded
        ? {
            id: 'hero-rec',
            title: recHook.items[0].title,
            subtitle: recHook.items[0].description || 'Specially curated for your viewing taste and display capabilities.',
            backgroundImageUrl: recHook.items[0].posterUrl || STATIC_FALLBACK_HERO.backgroundImageUrl,
            actionUrl: `/watch/${recHook.items[0].id}`,
            badge: recHook.tier === '4K' ? 'Personalized 4K Ultra Exclusive' : 'Personalized Standard HD'
          }
        : STATIC_FALLBACK_HERO)
    : evHook.layout.hero;

  return (
    <div className="min-h-screen flex flex-col bg-obsidian-900 text-slate-100">
      {/* Navigation & Telemetry Header */}
      <Header
        currentUserId={currentUserId}
        onUserChange={(userId) => {
          setCurrentUserId(userId);
          if (isCacheAside) recHook.refetch(userId);
          else evHook.refetch(userId);
        }}
        circuitState={circuitState}
        cacheStatus={cacheStatus}
        latencyMs={latencyMs}
        isDegraded={isDegraded}
        onRefresh={handleRefresh}
        isLoading={isLoading}
        activeMode={activeMode}
        onModeChange={setActiveMode}
      />

      {/* Main Container */}
      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-6">
        {/* Chaos HUD */}
        <ChaosControlPanel
          currentUserId={currentUserId}
          onRefresh={handleRefresh}
        />

        {/* Degraded Fallback Banner (Triggered when Circuit Breaker is OPEN) */}
        {isDegraded && isCacheAside && (
          <ResilientFallbackBanner
            reason={recHook.errorState}
            countdown={recHook.retryCountdown}
            onProbeNow={handleRefresh}
            isLoading={isLoading}
          />
        )}

        {/* Hero Section */}
        <HeroBanner
          hero={activeHero}
          isDegraded={isDegraded}
          tier={recHook.tier}
        />

        {/* Media Rows */}
        {isCacheAside ? (
          <RecommendationsCarousel
            title={isDegraded ? 'Popular Picks Across All Genres' : `AI Recommendations for You (${recHook.tier} Quality)`}
            category={isDegraded ? 'Static Fallback' : 'Live Curated'}
            items={recHook.items}
            isFallback={isDegraded}
          />
        ) : (
          <div>
            {evHook.layout.rows.map((row) => (
              <RecommendationsCarousel
                key={row.rowId}
                title={row.title}
                category={row.category}
                items={row.items}
                isFallback={evHook.layout.layoutType === 'GlobalDefaultFallback'}
              />
            ))}
          </div>
        )}
      </main>

      {/* Architectural Telemetry Footer */}
      <footer className="glass-panel border-t border-slate-800/80 mt-16 py-6 px-4">
        <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4 text-xs text-slate-400">
          <div className="flex items-center gap-6">
            <span className="flex items-center gap-1.5 font-medium text-slate-300">
              <ShieldCheck className="w-4 h-4 text-cyan-400" />
              Polly v8 Resilience Pipeline
            </span>
            <span className="flex items-center gap-1.5 font-medium text-slate-300">
              <Database className="w-4 h-4 text-emerald-400" />
              Redis Cache-Aside (Sliding TTL 60s)
            </span>
            <span className="flex items-center gap-1.5 font-medium text-slate-300">
              <Layers className="w-4 h-4 text-indigo-400" />
              Netflix-Style EVCache Primary Store
            </span>
          </div>
          <div className="flex items-center gap-2 font-mono text-[11px] text-slate-500">
            <Activity className="w-3.5 h-3.5 text-cyan-400" />
            <span>Active Target: {currentUserId} | Latency: {latencyMs}ms</span>
          </div>
        </div>
      </footer>
    </div>
  );
}

export default App;
