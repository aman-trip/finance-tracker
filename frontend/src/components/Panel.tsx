import type { PropsWithChildren, ReactNode } from "react";

type PanelProps = PropsWithChildren<{
  title: string;
  description?: string;
  actions?: ReactNode;
}>;

export const Panel = ({ title, description, actions, children }: PanelProps) => (
  <section className="rounded-[28px] border border-line bg-panel p-6 shadow-panel">
    <div className="mb-5 flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
      <div>
        <h2 className="font-display text-2xl text-ink">{title}</h2>
        {description ? <p className="mt-1 text-sm text-muted">{description}</p> : null}
      </div>
      {actions}
    </div>
    {children}
  </section>
);
