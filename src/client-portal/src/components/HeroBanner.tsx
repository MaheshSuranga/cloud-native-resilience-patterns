import React from 'react';
import { HomepageHeroBanner } from '../types/recommendations';
import { Play, Info, Sparkles, Volume2, Film } from 'lucide-react';

interface HeroBannerProps {
  hero: HomepageHeroBanner;
  isDegraded: boolean;
  tier: string;
}

export const HeroBanner: React.FC<HeroBannerProps> = ({ hero, isDegraded, tier }) => {
  return (
    <div className="relative rounded-3xl overflow-hidden glass-panel border border-slate-800 shadow-2xl mb-8 min-h-[380px] flex items-end">
      {/* Background Image & Cinematic Gradient Vignette */}
      <img
        src={hero.backgroundImageUrl}
        alt={hero.title}
        className="absolute inset-0 w-full h-full object-cover object-center filter brightness-[0.65] contrast-[1.1] transition-transform duration-1000 ease-out hover:scale-105"
      />
      <div className="absolute inset-0 bg-gradient-to-t from-obsidian-900 via-obsidian-900/60 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-r from-obsidian-900/90 via-obsidian-900/40 to-transparent" />

      {/* Content */}
      <div className="relative z-10 p-6 sm:p-10 max-w-2xl">
        {/* Tier / Status Pill */}
        <div className="flex flex-wrap items-center gap-2.5 mb-3.5">
          <span className={`inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold uppercase tracking-wider ${
            isDegraded
              ? 'bg-amber-500/20 text-amber-300 border border-amber-500/30'
              : 'bg-cyan-500/20 text-cyan-300 border border-cyan-500/30'
          }`}>
            <Sparkles className="w-3 h-3" />
            {hero.badge}
          </span>

          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-800/80 text-slate-300 border border-slate-700">
            <Film className="w-3 h-3 text-cyan-400" />
            {tier} Tier
          </span>

          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-medium bg-slate-800/80 text-slate-300 border border-slate-700">
            <Volume2 className="w-3 h-3 text-emerald-400" />
            Dolby Atmos
          </span>
        </div>

        {/* Title & Subtitle */}
        <h1 className="text-2xl sm:text-4xl font-extrabold tracking-tight text-white mb-2 leading-tight drop-shadow-md">
          {hero.title}
        </h1>
        <p className="text-slate-300 text-sm sm:text-base mb-6 line-clamp-2 leading-relaxed drop-shadow">
          {hero.subtitle}
        </p>

        {/* Action Buttons */}
        <div className="flex flex-wrap items-center gap-3">
          <button className="flex items-center gap-2 px-6 py-2.5 rounded-xl bg-gradient-to-r from-cyan-500 to-indigo-600 hover:from-cyan-400 hover:to-indigo-500 text-white font-semibold text-sm transition-all transform hover:scale-[1.02] shadow-lg shadow-cyan-500/25">
            <Play className="w-4 h-4 fill-white" />
            Stream Now
          </button>
          <button className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-slate-800/80 hover:bg-slate-700/80 text-slate-200 font-medium text-sm border border-slate-700 transition-all backdrop-blur-md">
            <Info className="w-4 h-4 text-slate-400" />
            More Details
          </button>
        </div>
      </div>
    </div>
  );
};
