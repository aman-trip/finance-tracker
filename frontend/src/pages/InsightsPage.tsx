import { useQuery } from "@tanstack/react-query";
import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Panel } from "../components/Panel";
import { StatsCard } from "../components/StatsCard";
import { financeService } from "../services/financeService";
import { formatCurrency } from "../utils/format";

const severityClasses: Record<string, string> = {
  HIGH: "border-danger/25 bg-danger/10 text-danger",
  MEDIUM: "border-amber-300/50 bg-amber-50 text-amber-700",
  LOW: "border-success/25 bg-success/10 text-success",
  INFO: "border-line/80 bg-white/75 text-muted",
};

export const InsightsPage = () => {
  const overviewQuery = useQuery({
    queryKey: ["insights", "overview"],
    queryFn: financeService.getInsightsOverview,
  });
  const forecastDailyQuery = useQuery({
    queryKey: ["forecast", "daily"],
    queryFn: financeService.getForecastDaily,
  });

  const healthScore = overviewQuery.data?.healthScore;

  return (
    <div className="space-y-8">
      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatsCard
          label="Health Score"
          value={`${healthScore?.score ?? 0}/100`}
          helper="Weighted across savings, stability, budget discipline, and cash runway."
          trend={
            (healthScore?.score ?? 0) >= 80
              ? "Strong profile"
              : (healthScore?.score ?? 0) >= 60
                ? "Stable but improvable"
                : "At-risk profile"
          }
          trendTone={
            (healthScore?.score ?? 0) >= 80
              ? "positive"
              : (healthScore?.score ?? 0) >= 60
                ? "neutral"
                : "negative"
          }
        />
        <StatsCard
          label="30-Day Forecast"
          value={formatCurrency(forecastDailyQuery.data?.forecastBalance ?? 0)}
          helper="Projected balance at the end of the daily forecast window."
          trend={forecastDailyQuery.data?.risk ?? "LOW"}
          trendTone={
            forecastDailyQuery.data?.risk === "HIGH"
              ? "negative"
              : forecastDailyQuery.data?.risk === "MEDIUM"
                ? "neutral"
                : "positive"
          }
        />
        <StatsCard
          label="Safe To Spend"
          value={formatCurrency(forecastDailyQuery.data?.safeToSpend ?? 0)}
          helper="Recommended daily discretionary spending based on reserves."
          trend="Daily guidance"
          trendTone="neutral"
        />
        <StatsCard
          label="Highlights"
          value={`${overviewQuery.data?.highlights.length ?? 0}`}
          helper="Personalized insights currently generated from your data."
          trend="Updated on refresh"
          trendTone="neutral"
        />
      </section>

      <Panel title="Health Score Breakdown" description="Each component contributes directly to the 0-100 financial health score.">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          {(healthScore?.breakdown ?? []).map((item) => (
            <article key={item.metric} className="rounded-2xl border border-line/70 bg-white/80 p-4 shadow-sm">
              <div className="flex items-center justify-between gap-3">
                <p className="font-semibold text-ink">{item.metric}</p>
                <p className="text-sm font-semibold text-ink">
                  {item.score}/{item.maxScore}
                </p>
              </div>
              <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-slate-200">
                <div
                  className="h-full rounded-full bg-gradient-to-r from-accent to-accent2"
                  style={{ width: `${Math.min(100, (item.score / item.maxScore) * 100)}%` }}
                />
              </div>
              <p className="mt-3 text-sm text-muted">{item.detail}</p>
            </article>
          ))}
        </div>
      </Panel>

      <Panel title="Daily Cash Flow Forecast" description="Projected balance and safe-to-spend guidance over the next 30 days.">
        <div className="h-96 w-full overflow-hidden rounded-2xl border border-line/70 bg-white/75 p-4 shadow-sm">
          <div className="h-full w-full min-w-0">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={forecastDailyQuery.data?.points ?? []} margin={{ top: 14, right: 16, left: 4, bottom: 8 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(126, 144, 173, 0.18)" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip
                  formatter={(value: number) => formatCurrency(value)}
                  contentStyle={{ borderRadius: "12px", borderColor: "rgba(126, 144, 173, 0.28)" }}
                />
                <Legend verticalAlign="top" iconType="circle" wrapperStyle={{ paddingBottom: "8px", fontSize: "12px" }} />
                <Line type="monotone" dataKey="projectedBalance" stroke="#1f5eff" strokeWidth={3} dot={false} />
                <Line type="monotone" dataKey="safeToSpend" stroke="#12a37d" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      </Panel>

      <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
        <Panel title="Suggestions" description="Direct actions that improve the health score and reduce forecast risk.">
          <div className="space-y-3">
            {(healthScore?.suggestions ?? []).map((suggestion) => (
              <div key={suggestion} className="rounded-2xl border border-line/70 bg-white/80 p-4 text-sm text-muted shadow-sm">
                {suggestion}
              </div>
            ))}
            {!healthScore?.suggestions.length ? <p className="text-sm text-muted">No suggestions available yet.</p> : null}
          </div>
        </Panel>

        <Panel title="Insight Highlights" description="Behavioral signals generated from budgets, trends, and recent spending.">
          <div className="grid gap-4 md:grid-cols-2">
            {(overviewQuery.data?.highlights ?? []).map((highlight, index) => (
              <article key={`${highlight.title}-${index}`} className="rounded-2xl border border-line/70 bg-white/80 p-4 shadow-sm">
                <div className="flex items-center justify-between gap-3">
                  <p className="text-sm font-semibold uppercase tracking-[0.12em] text-muted">{highlight.type}</p>
                  <span className={`rounded-full border px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.06em] ${severityClasses[highlight.severity] ?? severityClasses.INFO}`}>
                    {highlight.severity}
                  </span>
                </div>
                <h3 className="mt-2 font-semibold text-ink">{highlight.title}</h3>
                <p className="mt-1 text-sm text-muted">{highlight.description}</p>
              </article>
            ))}
            {!overviewQuery.data?.highlights.length ? <p className="text-sm text-muted">No insight highlights available yet.</p> : null}
          </div>
        </Panel>
      </div>
    </div>
  );
};
