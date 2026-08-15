import React from 'react';
import { ShieldAlert, RefreshCw, Activity } from 'lucide-react';

interface ResilientFallbackBannerProps {
  reason?: string | null;
  countdown: number;
  onProbeNow: () => void;
  isLoading: boolean;
}

export const ResilientFallbackBanner: React.FC<ResilientFallbackBannerProps> = ({
  reason,
  countdown,
  onProbeNow,
  isLoading
}) => {
  return (
    <div className="rounded-2xl p-4 sm:p-5 mb-8 bg-gradient-to-r from-amber-950/80 via-slate-900/90 to-amber-950/80 border border-amber-500/40 shadow-xl backdrop-blur-md">
      <div className="flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        {/* Left Status */}
        <div className="flex items-start gap-3.5">
          <div className="p-2.5 rounded-xl bg-amber-500/20 border border-amber-500/30 text-amber-400 shrink-0">
            <ShieldAlert className="w-6 h-6 animate-pulse" />
          </div>
          <div>
            <div className="flex items-center gap-2">
              <h3 className="font-bold text-amber-300 text-sm sm:text-base">
                Serving Popular Picks (Live Recommendations Temporarily Offline)
              </h3>
              <span className="px-2 py-0.5 rounded text-[10px] font-mono font-bold bg-amber-500/20 text-amber-300 border border-amber-500/30 uppercase">
                Graceful Fallback
              </span>
            </div>
            <p className="text-xs text-slate-300 mt-1 max-w-2xl leading-relaxed">
              {reason || 'Downstream microservice circuit breaker is OPEN. Fast fallback catalog is serving high-rating popular picks with 0ms downtime.'}
            </p>
          </div>
        </div>

        {/* Right Countdown & Probe Action */}
        <div className="flex items-center gap-3 w-full md:w-auto justify-between md:justify-end shrink-0 pt-2 md:pt-0 border-t md:border-t-0 border-amber-500/20">
          <div className="flex items-center gap-1.5 text-xs text-amber-200/90 font-mono bg-black/40 px-3 py-1.5 rounded-xl border border-amber-500/30">
            <Activity className="w-3.5 h-3.5 text-amber-400" />
            <span>Circuit Probe In:</span>
            <span className="font-bold text-amber-400 text-sm">{countdown}s</span>
          </div>

          <button
            onClick={onProbeNow}
            disabled={isLoading}
            className="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-amber-500 hover:bg-amber-400 text-slate-950 font-bold text-xs transition-colors shadow-lg shadow-amber-500/20 disabled:opacity-50"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
            Probe Now
          </button>
        </div>
      </div>
    </div>
  );
};
