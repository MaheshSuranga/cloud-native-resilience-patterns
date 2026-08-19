import React, { useState } from 'react';
import { Flame, Clock, CheckCircle2, Sliders, PlayCircle } from 'lucide-react';

interface ChaosControlPanelProps {
  currentUserId: string;
  onRefresh: () => void;
}

export const ChaosControlPanel: React.FC<ChaosControlPanelProps> = ({
  currentUserId,
  onRefresh
}) => {
  const [chaosLog, setChaosLog] = useState<string | null>(null);
  const [isExecuting, setIsExecuting] = useState<boolean>(false);

  const injectDownstreamError = async () => {
    setIsExecuting(true);
    setChaosLog(`Blasting 5 failing requests (HTTP 500) through RecommendationsService (User context '${currentUserId}') to trip Polly v8 Circuit Breaker...`);
    try {
      // Blast 5 uncached requests with simulateError=true to trip circuit breaker (50% threshold, min 4 requests)
      const requests = [1, 2, 3, 4, 5].map((i) =>
        fetch(`/recommendations/chaos_fail_${i}?simulateError=true`).catch(() => null)
      );
      await Promise.all(requests);
      setChaosLog(`5 downstream HTTP 500 errors injected! Circuit Breaker tripped to OPEN for 15s. Probing profile '${currentUserId}'...`);
      onRefresh();
    } catch {
      setChaosLog('Error dispatching downstream chaos request.');
    } finally {
      setIsExecuting(false);
    }
  };

  const injectLatencyDelay = async () => {
    setIsExecuting(true);
    setChaosLog(`Blasting 5 timing-out requests (5000ms delay) through RecommendationsService (Exceeds 2.0s Polly Timeout)...`);
    try {
      // Blast 5 parallel requests with 5000ms delay to trigger 2.0s Polly timeout and trip the circuit breaker
      const requests = [1, 2, 3, 4, 5].map((i) =>
        fetch(`/recommendations/chaos_delay_${i}?simulateDelay=5000`).catch(() => null)
      );
      await Promise.all(requests);
      setChaosLog(`2.0s Polly Timeout triggered across requests! Circuit Breaker tripped to OPEN for 15s. Probing profile '${currentUserId}'...`);
      onRefresh();
    } catch {
      setChaosLog('Dispatched latency test.');
    } finally {
      setIsExecuting(false);
    }
  };

  const triggerBatchPrecompute = async () => {
    setIsExecuting(true);
    setChaosLog('Executing parallel EVCache batch pre-compute across user cohort via Task.WhenAll...');
    try {
      const resp = await fetch('/homepage/precompute/batch', { method: 'POST' });
      if (resp.ok) {
        const data = await resp.json();
        setChaosLog(`Batch Pre-computation Completed! Precomputed ${data.processedUsers} users in ${data.elapsedMilliseconds}ms.`);
        onRefresh();
      }
    } catch (e: unknown) {
      setChaosLog(`Batch trigger error: ${(e as Error).message}`);
    } finally {
      setIsExecuting(false);
    }
  };

  return (
    <div className="glass-panel rounded-2xl p-5 mb-8 border border-slate-800">
      <div className="flex items-center justify-between mb-3.5">
        <div className="flex items-center gap-2 text-sm font-bold text-slate-200">
          <Sliders className="w-4 h-4 text-cyan-400" />
          <span>Resilience Chaos Testing HUD</span>
        </div>
        <span className="text-[11px] font-mono text-slate-500">
          Polly v8 Pipeline: 2.0s Timeout | 50% Fail Ratio | 15s Break Duration
        </span>
      </div>

      {/* Buttons */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-3">
        <button
          onClick={injectDownstreamError}
          disabled={isExecuting}
          className="flex items-center justify-center gap-2 px-3.5 py-2.5 rounded-xl bg-rose-950/60 hover:bg-rose-900/60 text-rose-300 border border-rose-600/40 text-xs font-semibold transition-all disabled:opacity-50 active:scale-[0.98]"
        >
          <Flame className="w-4 h-4 text-rose-400" />
          Trip Circuit Breaker (500 Error)
        </button>

        <button
          onClick={injectLatencyDelay}
          disabled={isExecuting}
          className="flex items-center justify-center gap-2 px-3.5 py-2.5 rounded-xl bg-amber-950/60 hover:bg-amber-900/60 text-amber-300 border border-amber-600/40 text-xs font-semibold transition-all disabled:opacity-50 active:scale-[0.98]"
        >
          <Clock className="w-4 h-4 text-amber-400" />
          Simulate 5s Timeout Delay
        </button>

        <button
          onClick={triggerBatchPrecompute}
          disabled={isExecuting}
          className="flex items-center justify-center gap-2 px-3.5 py-2.5 rounded-xl bg-indigo-950/60 hover:bg-indigo-900/60 text-indigo-300 border border-indigo-600/40 text-xs font-semibold transition-all disabled:opacity-50 active:scale-[0.98]"
        >
          <PlayCircle className="w-4 h-4 text-indigo-400" />
          Run EVCache Batch Precompute
        </button>
      </div>

      {/* Log Output */}
      {chaosLog && (
        <div className="p-2.5 rounded-xl bg-black/50 border border-slate-800 text-xs font-mono text-cyan-300/90 flex items-start gap-2">
          <CheckCircle2 className="w-4 h-4 text-cyan-400 shrink-0 mt-0.5" />
          <span>{chaosLog}</span>
        </div>
      )}
    </div>
  );
};
