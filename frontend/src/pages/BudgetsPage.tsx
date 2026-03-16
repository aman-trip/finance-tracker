import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { budgetSchema, type BudgetFormValues } from "../features/budgets/schema";
import { formatCurrency, monthName } from "../utils/format";

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
    mutationFn: (values: BudgetFormValues) => (editingId ? financeService.updateBudget(editingId, values) : financeService.createBudget(values)),
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
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {saveMutation.isPending ? "Saving..." : editingId ? "Update budget" : "Create budget"}
            </button>
            {editingId ? (
              <button
                type="button"
                className="rounded-full bg-white px-5 py-3 font-semibold text-ink"
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
          {(budgetsQuery.data ?? []).map((budget) => (
            <div key={budget.id} className="rounded-[24px] bg-white/70 p-5">
              <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                  <h3 className="font-semibold text-ink">{budget.categoryName}</h3>
                  <p className="text-sm text-muted">
                    {monthName(budget.month)} {budget.year} • Alert {budget.alertLevel}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-ink">
                    {formatCurrency(budget.spentAmount)} / {formatCurrency(budget.amount)}
                  </p>
                  <p className="text-sm text-muted">{budget.utilizationPercent.toFixed(1)}% used</p>
                </div>
              </div>
              <div className="mt-4 h-3 overflow-hidden rounded-full bg-slate-200">
                <div className="h-full rounded-full bg-accent2" style={{ width: `${Math.min(budget.utilizationPercent, 100)}%` }} />
              </div>
              <div className="mt-4 flex gap-3">
                <button
                  type="button"
                  className="rounded-full bg-white px-4 py-2 text-sm font-medium text-ink"
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
                  className="rounded-full bg-danger px-4 py-2 text-sm font-medium text-white"
                  onClick={() => deleteMutation.mutate(budget.id)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
          {!budgetsQuery.data?.length ? <p className="text-sm text-muted">No budgets configured yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
