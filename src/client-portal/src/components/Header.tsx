import React from 'react';
import { ResilienceTelemetryBadge } from './ResilienceTelemetryBadge';
import { CircuitBreakerState, CacheStatus } from '../types/recommendations';
import { Tv, User, RefreshCw, Cpu, Layers } from 'lucide-react';

interface HeaderProps {
  currentUserId: string;
  onUserChange: (userId: string) => void;
  circuitState: CircuitBreakerState;
  cacheStatus: CacheStatus;
  latencyMs: number;
  isDegraded: boolean;
  onRefresh: () => void;
  isLoading: boolean;
  activeMode: 'CacheAside' | 'EVCache';
  onModeChange: (mode: 'CacheAside' | 'EVCache') => void;
}

export const Header: React.FC<HeaderProps> = ({
  currentUserId,
  onUserChange,
  circuitState,
  cacheStatus,
  latencyMs,
  isDegraded,
  onRefresh,
  isLoading,
  activeMode,
  onModeChange
}) => {
  return (
    <header className="sticky top-0 z-50 glass-panel border-b border-slate-800/80 px-4 lg:px-8 py-3.5">
      <div className="max-w-7xl mx-auto flex flex-col md:flex-row items-center justify-between gap-4">
        {/* Brand & Mode Switcher */}
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-2.5">
            <div className="w-10 h-10 rounded-xl bg-gradient-to-tr from-cyan-500 via-indigo-600 to-emerald-400 flex items-center justify-center shadow-lg shadow-cyan-500/20">
              <Tv className="w-5 h-5 text-white" />
            </div>
            <div>
              <span className="font-extrabold text-xl tracking-tight bg-clip-text text-transparent bg-gradient-to-r from-white via-slate-200 to-cyan-400">
                StreamPulse
              </span>
              <span className="block text-[10px] uppercase font-mono tracking-widest text-cyan-400 font-semibold">
                Cloud Resilience Demo
              </span>
            </div>
          </div>

          {/* Mode Switcher */}
          <div className="hidden sm:flex items-center p-1 rounded-xl bg-slate-900/90 border border-slate-800 text-xs">
            <button
              onClick={() => onModeChange('CacheAside')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-all ${
                activeMode === 'CacheAside'
                  ? 'bg-cyan-600 text-white font-medium shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Cpu className="w-3.5 h-3.5" />
              Cache-Aside + Polly
            </button>
            <button
              onClick={() => onModeChange('EVCache')}
              className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg transition-all ${
                activeMode === 'EVCache'
                  ? 'bg-indigo-600 text-white font-medium shadow-sm'
                  : 'text-slate-400 hover:text-slate-200'
              }`}
            >
              <Layers className="w-3.5 h-3.5" />
              EVCache Primary Store
            </button>
          </div>
        </div>

        {/* Telemetry & User Profile */}
        <div className="flex items-center gap-3 w-full md:w-auto justify-between md:justify-end">
          <ResilienceTelemetryBadge
            circuitState={circuitState}
            cacheStatus={cacheStatus}
            latencyMs={latencyMs}
            isDegraded={isDegraded}
          />

          {/* User Switcher Dropdown */}
          <div className="flex items-center gap-2">
            <div className="relative inline-flex items-center bg-slate-900 border border-slate-700/80 rounded-xl px-2.5 py-1 text-xs">
              <User className="w-3.5 h-3.5 text-slate-400 mr-1.5" />
              <select
                value={currentUserId}
                onChange={(e) => onUserChange(e.target.value)}
                className="bg-transparent text-slate-200 font-medium focus:outline-none cursor-pointer pr-1"
              >
                <option value="user123" className="bg-slate-900">user123 (4K Ultra Tier)</option>
                <option value="user_std" className="bg-slate-900">user_std (Standard HD)</option>
                <option value="cold_user_99" className="bg-slate-900">cold_user_99 (Cold Cache)</option>
                <option value="vip_customer" className="bg-slate-900">vip_customer (VIP Cohort)</option>
              </select>
            </div>

            <button
              onClick={onRefresh}
              disabled={isLoading}
              className="p-2 rounded-xl bg-slate-800 hover:bg-slate-700 text-slate-300 transition-colors border border-slate-700 disabled:opacity-50"
              title="Refresh Stream Recommendations"
            >
              <RefreshCw className={`w-4 h-4 ${isLoading ? 'animate-spin text-cyan-400' : ''}`} />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
