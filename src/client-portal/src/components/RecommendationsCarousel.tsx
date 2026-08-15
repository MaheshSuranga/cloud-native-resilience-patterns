import React from 'react';
import { RecommendationItem } from '../types/recommendations';
import { Play, Star, Sparkles } from 'lucide-react';

interface RecommendationsCarouselProps {
  title: string;
  category?: string;
  items: RecommendationItem[];
  isFallback?: boolean;
}

export const RecommendationsCarousel: React.FC<RecommendationsCarouselProps> = ({
  title,
  category,
  items,
  isFallback = false
}) => {
  return (
    <section className="mb-10">
      {/* Row Header */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <h2 className="text-xl font-bold tracking-tight text-slate-100 flex items-center gap-2">
            {isFallback && <Sparkles className="w-4 h-4 text-amber-400" />}
            {title}
          </h2>
          {category && (
            <span className="text-xs px-2.5 py-0.5 rounded-full bg-slate-800 text-slate-400 font-medium border border-slate-700/60">
              {category}
            </span>
          )}
        </div>
        <span className="text-xs text-slate-500 font-mono">
          {items.length} titles available
        </span>
      </div>

      {/* Media Cards Grid */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
        {items.map((item) => (
          <div
            key={item.id}
            className="group relative rounded-2xl overflow-hidden glass-card flex flex-col justify-between"
          >
            {/* Poster & Gradient Overlay */}
            <div className="relative aspect-video w-full overflow-hidden bg-slate-900">
              <img
                src={item.posterUrl || 'https://images.unsplash.com/photo-1506703719100-a0f3a48c0f86?auto=format&fit=crop&w=600&q=80'}
                alt={item.title}
                className="w-full h-full object-cover transition-transform duration-500 ease-out group-hover:scale-110 filter brightness-90 group-hover:brightness-100"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-slate-950 via-transparent to-transparent opacity-90" />

              {/* Quality Badge */}
              <div className="absolute top-2.5 left-2.5">
                <span className="px-2 py-0.5 rounded-md text-[11px] font-bold bg-black/70 backdrop-blur-md text-cyan-300 border border-cyan-500/30 shadow">
                  {item.quality}
                </span>
              </div>

              {/* Match Score Badge */}
              <div className="absolute top-2.5 right-2.5">
                <span className="flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-bold bg-black/70 backdrop-blur-md text-emerald-400 border border-emerald-500/30 shadow">
                  <Star className="w-3 h-3 fill-emerald-400" />
                  {Math.round(item.score * 100)}%
                </span>
              </div>

              {/* Hover Quick-Play Icon */}
              <div className="absolute inset-0 flex items-center justify-center opacity-0 group-hover:opacity-100 transition-opacity bg-black/30 backdrop-blur-[2px]">
                <div className="w-12 h-12 rounded-full bg-cyan-500/90 text-white flex items-center justify-center shadow-lg transform group-hover:scale-110 transition-transform">
                  <Play className="w-5 h-5 fill-white ml-0.5" />
                </div>
              </div>
            </div>

            {/* Content Details */}
            <div className="p-4 flex-1 flex flex-col justify-between">
              <div>
                <span className="text-[11px] font-semibold text-slate-400 uppercase tracking-wider block mb-1">
                  {item.genre}
                </span>
                <h3 className="font-bold text-slate-100 text-base line-clamp-1 group-hover:text-cyan-300 transition-colors">
                  {item.title}
                </h3>
                {item.description && (
                  <p className="text-xs text-slate-400 line-clamp-2 mt-1.5 leading-relaxed">
                    {item.description}
                  </p>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
};
