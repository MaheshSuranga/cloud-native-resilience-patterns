import React from 'react';
import { CircuitBreakerState, CacheStatus } from '../types/recommendations';
import { ShieldCheck, ShieldAlert, AlertTriangle, Zap, Server } from 'lucide-react';

interface ResilienceTelemetryBadgeProps {
  circuitState: CircuitBreakerState;
  cacheStatus: CacheStatus;
  latencyMs: number;
  isDegraded: boolean;
}

export const ResilienceTelemetryBadge: React.FC<ResilienceTelemetryBadgeProps> = ({
  circuitState,
  cacheStatus,
  latencyMs,
  isDegraded
}) => {
  return (
    <div className="flex flex-wrap items-center gap-2 text-xs font-medium">
      {/* Circuit Breaker Status */}
      {circuitState === 'CLOSED' && !isDegraded && (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-950/70 text-emerald-400 border border-emerald-500/30 animate-glow-emerald">
          <ShieldCheck className="w-3.5 h-3.5" />
          <span className="font-semibold">Live Personalized</span>
          <span className="text-emerald-500/70 font-normal">| Circuit CLOSED</span>
        </span>
      )}

      {circuitState === 'OPEN' && (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-amber-950/70 text-amber-300 border border-amber-500/40 animate-glow-amber">
          <ShieldAlert className="w-3.5 h-3.5 text-amber-400" />
          <span className="font-semibold">Degraded Fallback Active</span>
          <span className="text-amber-400/70 font-normal">| Circuit OPEN</span>
        </span>
      )}

      {circuitState === 'HALF_OPEN' && (
        <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-cyan-950/70 text-cyan-300 border border-cyan-500/40 animate-pulse">
          <AlertTriangle className="w-3.5 h-3.5 text-cyan-400" />
          <span className="font-semibold">Probing Downstream</span>
          <span className="text-cyan-400/70 font-normal">| Circuit HALF-OPEN</span>
        </span>
      )}

      {/* Cache Status Badge */}
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-slate-800/80 text-slate-300 border border-slate-700">
        <Server className="w-3 h-3 text-cyan-400" />
        <span className="text-slate-400">Cache:</span>
        <span className={
          cacheStatus === 'HIT' || cacheStatus === 'PRECOMPUTED_PRIMARY'
            ? 'text-emerald-400 font-semibold'
            : cacheStatus === 'MISS'
            ? 'text-amber-400 font-semibold'
            : 'text-rose-400 font-semibold'
        }>
          {cacheStatus}
        </span>
      </span>

      {/* Latency Pill */}
      <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-slate-800/80 text-slate-300 border border-slate-700">
        <Zap className="w-3 h-3 text-yellow-400" />
        <span className="text-slate-400">Latency:</span>
        <span className="font-mono font-semibold text-slate-100">{latencyMs}ms</span>
      </span>
    </div>
  );
};
