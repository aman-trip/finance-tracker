import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { budgetSchema, type BudgetFormValues } from "../features/budgets/schema";
import { financeService } from "../services/financeService";
import { formatCurrency, monthName } from "../utils/format";

const budgetHealth = (usedPercent: number) => {
  if (usedPercent >= 90) {
    return {
      label: "Critical",
      tone: "Red",
      badgeClass: "border-red-200 bg-red-50 text-red-700",
      barClass: "bg-red-500",
    };
  }
  if (usedPercent >= 70) {
    return {
      label: "Warning",
      tone: "Yellow",
      badgeClass: "border-amber-200 bg-amber-50 text-amber-700",
      barClass: "bg-amber-500",
    };
  }
  return {
    label: "Healthy",
    tone: "Green",
    badgeClass: "border-emerald-200 bg-emerald-50 text-emerald-700",
    barClass: "bg-emerald-500",
  };
};

export const BudgetsPage = () => {
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const budgetsQuery = useQuery({ queryKey: ["budgets"], queryFn: financeService.getBudgets });
  const categoriesQuery = useQuery({ queryKey: ["categories"], queryFn: financeService.getCategories });

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<BudgetFormValues>({
    resolver: zodResolver(budgetSchema),
    defaultValues: {
      categoryId: "",
      month: new Date().getMonth() + 1,
      year: new Date().getFullYear(),
      amount: 0,
      alertThresholdPercent: 80,
    },
  });

  const saveMutation = useMutation({
    mutationFn: (values: BudgetFormValues) =>
      editingId ? financeService.updateBudget(editingId, values) : financeService.createBudget(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      setEditingId(null);
      reset({
        categoryId: "",
        month: new Date().getMonth() + 1,
        year: new Date().getFullYear(),
        amount: 0,
        alertThresholdPercent: 80,
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => financeService.deleteBudget(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["budgets"] }),
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[0.95fr_1.25fr]">
      <Panel title={editingId ? "Edit Budget" : "Create Budget"} description="Set category limits and watch 80%, 100%, and 120% thresholds.">
        <form className="grid gap-4" onSubmit={handleSubmit((values) => saveMutation.mutate(values))}>
          <div>
            <label className="mb-2 block text-sm font-medium">Expense category</label>
            <select {...register("categoryId")}>
              <option value="">Select category</option>
              {(categoriesQuery.data ?? [])
                .filter((category) => category.type === "EXPENSE")
                .map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
            </select>
            {errors.categoryId ? <p className="mt-1 text-sm text-danger">{errors.categoryId.message}</p> : null}
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Month</label>
              <input type="number" {...register("month")} />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Year</label>
              <input type="number" {...register("year")} />
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Budget Amount</label>
              <input type="number" step="0.01" {...register("amount")} />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Alert Threshold (%)</label>
              <input type="number" {...register("alertThresholdPercent")} />
            </div>
          </div>
          <div className="flex gap-3">
            <button
              type="submit"
              className="rounded-full bg-accent px-5 py-2.5 text-sm font-semibold text-white shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
            >
              {saveMutation.isPending ? "Saving..." : editingId ? "Update budget" : "Create budget"}
            </button>
            {editingId ? (
              <button
                type="button"
                className="rounded-full border border-line/80 bg-white px-5 py-2.5 text-sm font-semibold text-ink shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
                onClick={() => {
                  setEditingId(null);
                  reset();
                }}
              >
                Cancel
              </button>
            ) : null}
          </div>
        </form>
      </Panel>

      <Panel title="Budget Monitoring" description="Review utilization, spent amounts, and alert levels.">
        <div className="space-y-4">
          {(budgetsQuery.data ?? []).map((budget) => {
            const usedPercent =
              budget.amount > 0 ? Number(((budget.spentAmount / budget.amount) * 100).toFixed(1)) : budget.utilizationPercent;
            const progressWidth = Math.min(Math.max(usedPercent, 0), 100);
            const health = budgetHealth(usedPercent);

            return (
              <div key={budget.id} className="rounded-2xl border border-line/70 bg-white/80 p-4 shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-lift">
                <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                  <div className="space-y-1.5">
                    <h3 className="font-semibold text-ink">{budget.categoryName}</h3>
                    <p className="text-sm text-muted">
                      {monthName(budget.month)} {budget.year} - Alert {budget.alertLevel}
                    </p>
                    <span className={`inline-flex rounded-full border px-2.5 py-1 text-[11px] font-semibold uppercase tracking-[0.06em] ${health.badgeClass}`}>
                      {health.tone} - {health.label}
                    </span>
                  </div>
                  <div className="text-left md:text-right">
                    <p className="font-semibold text-ink">
                      {formatCurrency(budget.spentAmount)} / {formatCurrency(budget.amount)}
                    </p>
                    <p className="text-sm text-muted">{usedPercent.toFixed(1)}% used</p>
                  </div>
                </div>
                <div className="mt-4 h-3 overflow-hidden rounded-full bg-slate-200">
                  <div
                    className={`h-full rounded-full transition-all duration-300 ${health.barClass}`}
                    style={{ width: `${progressWidth}%` }}
                  />
                </div>
                <div className="mt-4 flex flex-wrap gap-3">
                  <button
                    type="button"
                    className="rounded-full border border-line/80 bg-white px-4 py-2 text-sm font-semibold text-ink shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
                    onClick={() => {
                      setEditingId(budget.id);
                      setValue("categoryId", budget.categoryId);
                      setValue("month", budget.month);
                      setValue("year", budget.year);
                      setValue("amount", budget.amount);
                      setValue("alertThresholdPercent", budget.alertThresholdPercent);
                    }}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="rounded-full bg-danger px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md"
                    onClick={() => deleteMutation.mutate(budget.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>
            );
          })}
          {!budgetsQuery.data?.length ? <p className="text-sm text-muted">No budgets configured yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
