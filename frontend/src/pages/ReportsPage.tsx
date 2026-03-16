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

const colors = ["#1f8a70", "#f4a261", "#457b9d", "#d1495b", "#2a9d8f", "#6d597a"];

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
    <div className="space-y-6">
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
              className="rounded-full bg-white px-5 py-3 font-semibold text-ink"
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

      <div className="grid gap-6 xl:grid-cols-[1fr_1fr]">
        <Panel title="Category Spend Breakdown" description="Expense concentration by category.">
          <div className="h-96">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={categorySpendQuery.data?.items ?? []} dataKey="amount" nameKey="category" outerRadius={130}>
                  {(categorySpendQuery.data?.items ?? []).map((entry, index) => (
                    <Cell key={entry.category} fill={colors[index % colors.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>
          <p className="text-sm text-muted">Total spend: {formatCurrency(categorySpendQuery.data?.total ?? 0)}</p>
        </Panel>

        <Panel title="Income vs Expense Trend" description="Daily trend line for income and expense totals.">
          <div className="h-96">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={trendQuery.data ?? []}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(109, 128, 138, 0.15)" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Legend />
                <Line type="monotone" dataKey="income" stroke="#1f8a70" strokeWidth={3} dot={false} />
                <Line type="monotone" dataKey="expense" stroke="#d1495b" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Panel>
      </div>

      <Panel title="Account Balance Trend" description="Per-account running balances across the selected range.">
        <div className="grid gap-6 xl:grid-cols-2">
          {(balanceTrendQuery.data ?? []).map((series, index) => (
            <div key={series.accountName} className="rounded-[24px] bg-white/70 p-4">
              <p className="mb-3 font-semibold text-ink">{series.accountName}</p>
              <div className="h-72">
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={series.points}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(109, 128, 138, 0.15)" />
                    <XAxis dataKey="date" />
                    <YAxis />
                    <Tooltip formatter={(value: number) => formatCurrency(value)} />
                    <Line type="monotone" dataKey="balance" stroke={colors[index % colors.length]} strokeWidth={3} dot={false} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>
          ))}
        </div>
      </Panel>
    </div>
  );
};
