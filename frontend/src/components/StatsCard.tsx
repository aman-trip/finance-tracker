import type { ReactNode } from "react";

type StatsCardProps = {
  label: string;
  value: string;
  helper?: string;
  trend?: string;
  trendTone?: "positive" | "negative" | "neutral";
  icon?: ReactNode;
};

const trendToneClasses = {
  positive: "border-success/25 bg-success/10 text-success",
  negative: "border-danger/25 bg-danger/10 text-danger",
  neutral: "border-line/80 bg-white/75 text-muted dark:bg-slate-800/70 dark:text-slate-300",
};

const iconToneClasses = {
  positive: "text-success",
  negative: "text-danger",
  neutral: "text-muted dark:text-slate-300",
};

export const StatsCard = ({ label, value, helper, trend, trendTone = "neutral", icon }: StatsCardProps) => (
  <div className="group rounded-[24px] border border-line/80 bg-panel p-5 shadow-panel transition-all duration-300 hover:-translate-y-1 hover:border-accent/35 hover:shadow-lift dark:border-slate-700/70 dark:bg-slate-900/70">
    <div className="flex items-start justify-between gap-3">
      <p className="text-xs uppercase tracking-[0.24em] text-muted dark:text-slate-300">{label}</p>
      {icon ? <span className={`inline-flex items-center ${iconToneClasses[trendTone]}`}>{icon}</span> : null}
    </div>
    <p className="mt-4 font-display text-3xl font-semibold text-ink">{value}</p>
    {helper ? <p className="mt-2 text-sm text-muted dark:text-slate-300">{helper}</p> : null}
    {trend ? (
      <div className={`mt-3 inline-flex rounded-full border px-2.5 py-1 text-xs font-semibold ${trendToneClasses[trendTone]}`}>{trend}</div>
    ) : null}
    <div className="mt-4 h-1 w-14 rounded-full bg-gradient-to-r from-accent to-accent2 opacity-75 transition-all duration-300 group-hover:w-24" />
  </div>
);
