import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { goalMovementSchema, goalSchema, type GoalFormValues, type GoalMovementFormValues } from "../features/goals/schema";
import { goalStatusOptions } from "../utils/constants";
import { formatCurrency, formatDate } from "../utils/format";

export const GoalsPage = () => {
  const queryClient = useQueryClient();
  const goalsQuery = useQuery({ queryKey: ["goals"], queryFn: financeService.getGoals });
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });

  const goalForm = useForm<GoalFormValues>({
    resolver: zodResolver(goalSchema),
    defaultValues: {
      name: "",
      targetAmount: 0,
      targetDate: "",
      status: "ACTIVE",
    },
  });

  const movementForm = useForm<GoalMovementFormValues>({
    resolver: zodResolver(goalMovementSchema),
    defaultValues: {
      goalId: "",
      accountId: "",
      amount: 0,
      direction: "contribute",
    },
  });

  const createGoalMutation = useMutation({
    mutationFn: (values: GoalFormValues) =>
      financeService.createGoal({
        ...values,
        targetDate: values.targetDate || null,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["goals"] });
      goalForm.reset();
    },
  });

  const movementMutation = useMutation({
    mutationFn: (values: GoalMovementFormValues) =>
      values.direction === "contribute"
        ? financeService.contributeToGoal(values.goalId, { accountId: values.accountId, amount: values.amount })
        : financeService.withdrawFromGoal(values.goalId, { accountId: values.accountId, amount: values.amount }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["goals"] });
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      movementForm.reset();
    },
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[0.95fr_1.25fr]">
      <div className="space-y-6">
        <Panel title="Create Savings Goal" description="Define a target amount, optional date, and status.">
          <form className="grid gap-4" onSubmit={goalForm.handleSubmit((values) => createGoalMutation.mutate(values))}>
            <div>
              <label className="mb-2 block text-sm font-medium">Goal name</label>
              <input {...goalForm.register("name")} />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Target amount</label>
                <input type="number" step="0.01" {...goalForm.register("targetAmount")} />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Target date</label>
                <input type="date" {...goalForm.register("targetDate")} />
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Status</label>
              <select {...goalForm.register("status")}>
                {goalStatusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {createGoalMutation.isPending ? "Saving..." : "Create goal"}
            </button>
          </form>
        </Panel>

        <Panel title="Move Goal Funds" description="Contribute from an account or withdraw funds back into one.">
          <form className="grid gap-4" onSubmit={movementForm.handleSubmit((values) => movementMutation.mutate(values))}>
            <div>
              <label className="mb-2 block text-sm font-medium">Goal</label>
              <select {...movementForm.register("goalId")}>
                <option value="">Select goal</option>
                {(goalsQuery.data ?? []).map((goal) => (
                  <option key={goal.id} value={goal.id}>
                    {goal.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Account</label>
                <select {...movementForm.register("accountId")}>
                  <option value="">Select account</option>
                  {(accountsQuery.data ?? []).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Amount</label>
                <input type="number" step="0.01" {...movementForm.register("amount")} />
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Action</label>
              <select {...movementForm.register("direction")}>
                <option value="contribute">Contribute</option>
                <option value="withdraw">Withdraw</option>
              </select>
            </div>
            <button type="submit" className="rounded-full bg-ink px-5 py-3 font-semibold text-white">
              {movementMutation.isPending ? "Processing..." : "Submit"}
            </button>
          </form>
        </Panel>
      </div>

      <Panel title="Goals Overview" description="Track progress, status, and deadlines.">
        <div className="space-y-4">
          {(goalsQuery.data ?? []).map((goal) => (
            <div key={goal.id} className="rounded-[24px] bg-white/70 p-5">
              <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                  <h3 className="font-semibold text-ink">{goal.name}</h3>
                  <p className="text-sm text-muted">
                    {goal.targetDate ? `Target ${formatDate(goal.targetDate)}` : "No target date"} • {goal.status}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-ink">
                    {formatCurrency(goal.currentAmount)} / {formatCurrency(goal.targetAmount)}
                  </p>
                  <p className="text-sm text-muted">{goal.progressPercent.toFixed(1)}% complete</p>
                </div>
              </div>
              <div className="mt-4 h-3 overflow-hidden rounded-full bg-slate-200">
                <div className="h-full rounded-full bg-accent" style={{ width: `${Math.min(goal.progressPercent, 100)}%` }} />
              </div>
            </div>
          ))}
          {!goalsQuery.data?.length ? <p className="text-sm text-muted">No savings goals defined yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
