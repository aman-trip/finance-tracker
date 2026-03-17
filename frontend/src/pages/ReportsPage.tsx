import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  CartesianGrid,
  Cell,
  Legend,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { buildMonthToDateRange } from "../features/reports/dateRange";
import { formatCurrency } from "../utils/format";

const colors = ["#1f5eff", "#32a0ff", "#3f67d9", "#12a37d", "#5c7bdb", "#92b0ff"];

export const ReportsPage = () => {
  const defaultRange = buildMonthToDateRange();
  const [startDate, setStartDate] = useState(defaultRange.startDate);
  const [endDate, setEndDate] = useState(defaultRange.endDate);

  const params = { startDate, endDate };
  const categorySpendQuery = useQuery({
    queryKey: ["reports", "category-spend", params],
    queryFn: () => financeService.getCategorySpend(params),
  });
  const trendQuery = useQuery({
    queryKey: ["reports", "income-expense", params],
    queryFn: () => financeService.getIncomeExpenseTrend(params),
  });
  const balanceTrendQuery = useQuery({
    queryKey: ["reports", "account-balance", params],
    queryFn: () => financeService.getAccountBalanceTrend(params),
  });

  return (
    <div className="space-y-8">
      <Panel title="Report Filters" description="Adjust the reporting window for every chart on this page.">
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
          <div>
            <label className="mb-2 block text-sm font-medium">Start date</label>
            <input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} />
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium">End date</label>
            <input type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} />
          </div>
          <div className="md:col-span-2 flex items-end">
            <button
              type="button"
              className="rounded-full border border-line/80 bg-white px-5 py-2.5 text-sm font-semibold text-ink shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
              onClick={() => {
                const range = buildMonthToDateRange();
                setStartDate(range.startDate);
                setEndDate(range.endDate);
              }}
            >
              Reset to month-to-date
            </button>
          </div>
        </div>
      </Panel>

      <div className="grid items-start gap-6 xl:grid-cols-[1fr_1fr]">
        <Panel title="Category Spend Breakdown" description="Expense concentration by category.">
          <div className="h-96 w-full overflow-hidden rounded-2xl border border-line/70 bg-white/75 p-4 shadow-sm transition-all duration-300 hover:shadow-md">
            <div className="h-full w-full min-w-0">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={categorySpendQuery.data?.items ?? []}
                    dataKey="amount"
                    nameKey="category"
                    innerRadius="45%"
                    outerRadius="72%"
                    paddingAngle={2}
                    stroke="rgba(255,255,255,0.88)"
                    strokeWidth={3}
                  >
                    {(categorySpendQuery.data?.items ?? []).map((entry, index) => (
                      <Cell key={entry.category} fill={colors[index % colors.length]} />
                    ))}
                  </Pie>
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    contentStyle={{ borderRadius: "12px", borderColor: "rgba(126, 144, 173, 0.28)" }}
                  />
                  <Legend verticalAlign="bottom" iconType="circle" wrapperStyle={{ fontSize: "12px" }} />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </div>
          <div className="rounded-2xl border border-line/70 bg-white/80 px-3 py-2 text-sm text-muted shadow-sm">
            Total spend: <span className="font-semibold text-ink">{formatCurrency(categorySpendQuery.data?.total ?? 0)}</span>
          </div>
        </Panel>

        <Panel title="Income vs Expense Trend" description="Daily trend line for income and expense totals.">
          <div className="h-96 w-full overflow-hidden rounded-2xl border border-line/70 bg-white/75 p-4 shadow-sm transition-all duration-300 hover:shadow-md">
            <div className="h-full w-full min-w-0">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={trendQuery.data ?? []} margin={{ top: 14, right: 16, left: 4, bottom: 8 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(126, 144, 173, 0.18)" />
                  <XAxis dataKey="date" />
                  <YAxis />
                  <Tooltip
                    formatter={(value: number) => formatCurrency(value)}
                    contentStyle={{ borderRadius: "12px", borderColor: "rgba(126, 144, 173, 0.28)" }}
                  />
                  <Legend verticalAlign="top" iconType="circle" wrapperStyle={{ paddingBottom: "8px", fontSize: "12px" }} />
                  <Line type="monotone" dataKey="income" stroke="#1f5eff" strokeWidth={3} dot={false} />
                  <Line type="monotone" dataKey="expense" stroke="#d55353" strokeWidth={3} dot={false} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </div>
        </Panel>
      </div>

      <Panel title="Account Balance Trend" description="Per-account running balances across the selected range.">
        <div className="grid items-start gap-6 xl:grid-cols-2">
          {(balanceTrendQuery.data ?? []).map((series, index) => (
            <div key={series.accountName} className="rounded-[24px] border border-line/70 bg-white/80 p-4 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-lift">
              <p className="mb-3 font-semibold text-ink">{series.accountName}</p>
              <div className="h-72 w-full overflow-hidden rounded-2xl border border-line/70 bg-white/75 p-4">
                <div className="h-full w-full min-w-0">
                  <ResponsiveContainer width="100%" height="100%">
                    <LineChart data={series.points} margin={{ top: 14, right: 16, left: 4, bottom: 8 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="rgba(126, 144, 173, 0.18)" />
                      <XAxis dataKey="date" />
                      <YAxis />
                      <Tooltip
                        formatter={(value: number) => formatCurrency(value)}
                        contentStyle={{ borderRadius: "12px", borderColor: "rgba(126, 144, 173, 0.28)" }}
                      />
                      <Line type="monotone" dataKey="balance" stroke={colors[index % colors.length]} strokeWidth={3} dot={false} />
                    </LineChart>
                  </ResponsiveContainer>
                </div>
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </div>
  );
};
