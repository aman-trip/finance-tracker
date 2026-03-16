import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { Cell, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Panel } from "../components/Panel";
import { StatsCard } from "../components/StatsCard";
import { financeService } from "../services/financeService";
import { buildMonthToDateRange } from "../features/reports/dateRange";
import { formatCurrency, formatDate } from "../utils/format";

const chartColors = ["#1f8a70", "#f4a261", "#457b9d", "#d1495b", "#2a9d8f", "#6d597a"];

export const DashboardPage = () => {
  const range = useMemo(() => buildMonthToDateRange(), []);
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });
  const budgetsQuery = useQuery({ queryKey: ["budgets"], queryFn: financeService.getBudgets });
  const goalsQuery = useQuery({ queryKey: ["goals"], queryFn: financeService.getGoals });
  const recurringQuery = useQuery({ queryKey: ["recurring"], queryFn: financeService.getRecurring });
  const transactionsQuery = useQuery({
    queryKey: ["transactions", "recent"],
    queryFn: () => financeService.getTransactions({ page: 0, size: 5 }),
  });
  const categorySpendQuery = useQuery({
    queryKey: ["reports", "category-spend", range],
    queryFn: () => financeService.getCategorySpend(range),
  });
  const trendQuery = useQuery({
    queryKey: ["reports", "income-expense", range],
    queryFn: () => financeService.getIncomeExpenseTrend(range),
  });

  const totals = useMemo(() => {
    const items = trendQuery.data ?? [];
    const income = items.reduce((sum, item) => sum + item.income, 0);
    const expense = items.reduce((sum, item) => sum + item.expense, 0);
    return { income, expense, net: income - expense };
  }, [trendQuery.data]);

  const accountBalance = (accountsQuery.data ?? []).reduce((sum, account) => sum + account.currentBalance, 0);

  return (
    <div className="space-y-6">
      <section className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatsCard label="Month Income" value={formatCurrency(totals.income)} />
        <StatsCard label="Month Expense" value={formatCurrency(totals.expense)} />
        <StatsCard label="Net Balance" value={formatCurrency(totals.net)} helper="Income minus expense for the current month." />
        <StatsCard label="All Accounts" value={formatCurrency(accountBalance)} helper={`${accountsQuery.data?.length ?? 0} tracked accounts`} />
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
        <Panel title="Spending by Category" description="Current month expense allocation across categories.">
          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie data={categorySpendQuery.data?.items ?? []} dataKey="amount" nameKey="category" innerRadius={70} outerRadius={110}>
                  {(categorySpendQuery.data?.items ?? []).map((entry, index) => (
                    <Cell key={entry.category} fill={chartColors[index % chartColors.length]} />
                  ))}
                </Pie>
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </Panel>

        <Panel title="Goal Progress" description="Savings goals and completion pace.">
          <div className="space-y-4">
            {(goalsQuery.data ?? []).slice(0, 4).map((goal) => (
              <div key={goal.id} className="rounded-2xl bg-white/70 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink">{goal.name}</p>
                    <p className="text-sm text-muted">{goal.targetDate ? `Target ${formatDate(goal.targetDate)}` : "No target date"}</p>
                  </div>
                  <p className="text-sm font-medium text-ink">{goal.progressPercent.toFixed(0)}%</p>
                </div>
                <div className="mt-3 h-3 overflow-hidden rounded-full bg-slate-200">
                  <div className="h-full rounded-full bg-accent" style={{ width: `${Math.min(goal.progressPercent, 100)}%` }} />
                </div>
              </div>
            ))}
            {!goalsQuery.data?.length ? <p className="text-sm text-muted">No goals yet.</p> : null}
          </div>
        </Panel>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.4fr_1fr]">
        <Panel title="Income vs Expense Trend" description="Daily totals for the current month.">
          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={trendQuery.data ?? []}>
                <XAxis dataKey="date" tickLine={false} axisLine={false} />
                <YAxis tickFormatter={(value) => `$${value}`} tickLine={false} axisLine={false} />
                <Tooltip formatter={(value: number) => formatCurrency(value)} />
                <Line type="monotone" dataKey="income" stroke="#1f8a70" strokeWidth={3} dot={false} />
                <Line type="monotone" dataKey="expense" stroke="#d1495b" strokeWidth={3} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </Panel>

        <Panel title="Upcoming Recurring" description="Next scheduled items waiting to post.">
          <div className="space-y-3">
            {(recurringQuery.data ?? []).slice(0, 5).map((item) => (
              <div key={item.id} className="rounded-2xl bg-white/70 p-4">
                <p className="font-semibold text-ink">{item.title}</p>
                <p className="mt-1 text-sm text-muted">
                  {item.frequency} on {formatDate(item.nextRunDate)} from {item.accountName}
                </p>
                <p className="mt-2 text-sm font-medium text-ink">{formatCurrency(item.amount)}</p>
              </div>
            ))}
            {!recurringQuery.data?.length ? <p className="text-sm text-muted">No recurring transactions scheduled.</p> : null}
          </div>
        </Panel>
      </section>

      <section className="grid gap-6 xl:grid-cols-[1.1fr_1fr]">
        <Panel title="Budget Progress" description="Monitor monthly budget utilization and alerts.">
          <div className="space-y-4">
            {(budgetsQuery.data ?? []).slice(0, 5).map((budget) => (
              <div key={budget.id} className="rounded-2xl bg-white/70 p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="font-semibold text-ink">{budget.categoryName}</p>
                    <p className="text-sm text-muted">
                      {budget.month}/{budget.year} • Alert {budget.alertLevel}
                    </p>
                  </div>
                  <p className="text-sm font-medium text-ink">{formatCurrency(budget.spentAmount)} / {formatCurrency(budget.amount)}</p>
                </div>
                <div className="mt-3 h-3 overflow-hidden rounded-full bg-slate-200">
                  <div className="h-full rounded-full bg-accent2" style={{ width: `${Math.min(budget.utilizationPercent, 100)}%` }} />
                </div>
              </div>
            ))}
            {!budgetsQuery.data?.length ? <p className="text-sm text-muted">No budgets configured.</p> : null}
          </div>
        </Panel>

        <Panel title="Recent Transactions" description="Latest transaction activity across all accounts.">
          <div className="space-y-3">
            {(transactionsQuery.data?.content ?? []).map((transaction) => (
              <div key={transaction.id} className="rounded-2xl bg-white/70 p-4">
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <p className="font-semibold text-ink">{transaction.merchant || transaction.categoryName || transaction.type}</p>
                    <p className="text-sm text-muted">
                      {transaction.accountName} • {formatDate(transaction.transactionDate)}
                    </p>
                  </div>
                  <p className={`font-semibold ${transaction.type === "INCOME" ? "text-success" : "text-danger"}`}>
                    {transaction.type === "INCOME" ? "+" : "-"}
                    {formatCurrency(transaction.amount)}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </Panel>
      </section>
    </div>
  );
};
