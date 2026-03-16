import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { recurringFrequencyOptions, transactionTypeOptions } from "../utils/constants";
import { formatCurrency, formatDate } from "../utils/format";

type RecurringFormValues = {
  title: string;
  type: "INCOME" | "EXPENSE";
  amount: number;
  categoryId?: string;
  accountId: string;
  frequency: "DAILY" | "WEEKLY" | "MONTHLY" | "YEARLY";
  startDate: string;
  endDate?: string;
  nextRunDate: string;
  autoCreateTransaction: boolean;
};

export const RecurringPage = () => {
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const recurringQuery = useQuery({ queryKey: ["recurring"], queryFn: financeService.getRecurring });
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });
  const categoriesQuery = useQuery({ queryKey: ["categories"], queryFn: financeService.getCategories });

  const form = useForm<RecurringFormValues>({
    defaultValues: {
      title: "",
      type: "EXPENSE",
      amount: 0,
      categoryId: "",
      accountId: "",
      frequency: "MONTHLY",
      startDate: new Date().toISOString().slice(0, 10),
      endDate: "",
      nextRunDate: new Date().toISOString().slice(0, 10),
      autoCreateTransaction: true,
    },
  });

  const saveMutation = useMutation({
    mutationFn: (values: RecurringFormValues) => {
      const payload = {
        ...values,
        categoryId: values.categoryId || null,
        endDate: values.endDate || null,
      };
      return editingId ? financeService.updateRecurring(editingId, payload) : financeService.createRecurring(payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recurring"] });
      form.reset();
      setEditingId(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => financeService.deleteRecurring(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["recurring"] }),
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[0.95fr_1.25fr]">
      <Panel title={editingId ? "Edit Recurring Rule" : "Create Recurring Rule"} description="Automate salary, rent, subscriptions, and other repeated transactions.">
        <form className="grid gap-4" onSubmit={form.handleSubmit((values) => saveMutation.mutate(values))}>
          <div>
            <label className="mb-2 block text-sm font-medium">Title</label>
            <input {...form.register("title", { required: true })} />
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Type</label>
              <select {...form.register("type")}>
                {transactionTypeOptions.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Amount</label>
              <input type="number" step="0.01" {...form.register("amount", { valueAsNumber: true })} />
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Account</label>
              <select {...form.register("accountId", { required: true })}>
                <option value="">Select account</option>
                {(accountsQuery.data ?? []).map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Category</label>
              <select {...form.register("categoryId")}>
                <option value="">Optional category</option>
                {(categoriesQuery.data ?? []).map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-3">
            <div>
              <label className="mb-2 block text-sm font-medium">Frequency</label>
              <select {...form.register("frequency")}>
                {recurringFrequencyOptions.map((frequency) => (
                  <option key={frequency} value={frequency}>
                    {frequency}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Start date</label>
              <input type="date" {...form.register("startDate")} />
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Next run date</label>
              <input type="date" {...form.register("nextRunDate")} />
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">End date</label>
              <input type="date" {...form.register("endDate")} />
            </div>
            <label className="flex items-center gap-3 rounded-2xl bg-white/70 px-4 py-3">
              <input type="checkbox" className="h-5 w-5" {...form.register("autoCreateTransaction")} />
              <span className="text-sm font-medium text-ink">Automatically create transactions</span>
            </label>
          </div>
          <div className="flex gap-3">
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {saveMutation.isPending ? "Saving..." : editingId ? "Update rule" : "Create rule"}
            </button>
            {editingId ? (
              <button
                type="button"
                className="rounded-full bg-white px-5 py-3 font-semibold text-ink"
                onClick={() => {
                  setEditingId(null);
                  form.reset();
                }}
              >
                Cancel
              </button>
            ) : null}
          </div>
        </form>
      </Panel>

      <Panel title="Recurring Schedule" description="Upcoming rules that the backend scheduler will process automatically.">
        <div className="space-y-4">
          {(recurringQuery.data ?? []).map((item) => (
            <div key={item.id} className="rounded-[24px] bg-white/70 p-5">
              <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                  <h3 className="font-semibold text-ink">{item.title}</h3>
                  <p className="text-sm text-muted">
                    {item.frequency} • Next run {formatDate(item.nextRunDate)} • {item.accountName}
                  </p>
                </div>
                <div className="text-right">
                  <p className="font-semibold text-ink">{formatCurrency(item.amount)}</p>
                  <p className="text-sm text-muted">{item.autoCreateTransaction ? "Auto create on" : "Auto create off"}</p>
                </div>
              </div>
              <div className="mt-4 flex gap-3">
                <button
                  type="button"
                  className="rounded-full bg-white px-4 py-2 text-sm font-medium text-ink"
                  onClick={() => {
                    setEditingId(item.id);
                    form.setValue("title", item.title);
                    form.setValue("type", item.type === "INCOME" ? "INCOME" : "EXPENSE");
                    form.setValue("amount", item.amount);
                    form.setValue("categoryId", item.categoryId ?? "");
                    form.setValue("accountId", item.accountId);
                    form.setValue("frequency", item.frequency);
                    form.setValue("startDate", item.startDate);
                    form.setValue("endDate", item.endDate ?? "");
                    form.setValue("nextRunDate", item.nextRunDate);
                    form.setValue("autoCreateTransaction", item.autoCreateTransaction);
                  }}
                >
                  Edit
                </button>
                <button
                  type="button"
                  className="rounded-full bg-danger px-4 py-2 text-sm font-medium text-white"
                  onClick={() => deleteMutation.mutate(item.id)}
                >
                  Delete
                </button>
              </div>
            </div>
          ))}
          {!recurringQuery.data?.length ? <p className="text-sm text-muted">No recurring rules yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
