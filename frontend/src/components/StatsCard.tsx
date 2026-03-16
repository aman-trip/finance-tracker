type StatsCardProps = {
  label: string;
  value: string;
  helper?: string;
};

export const StatsCard = ({ label, value, helper }: StatsCardProps) => (
  <div className="rounded-[24px] border border-line bg-panel p-5 shadow-panel">
    <p className="text-sm uppercase tracking-[0.2em] text-muted">{label}</p>
    <p className="mt-4 font-display text-3xl text-ink">{value}</p>
    {helper ? <p className="mt-2 text-sm text-muted">{helper}</p> : null}
  </div>
);
