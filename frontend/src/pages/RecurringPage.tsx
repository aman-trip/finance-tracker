import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { extractApiError } from "../utils/apiError";
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
  const [formApiError, setFormApiError] = useState<string | null>(null);
  const recurringQuery = useQuery({ queryKey: ["recurring"], queryFn: financeService.getRecurring });
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });
  const categoriesQuery = useQuery({ queryKey: ["categories"], queryFn: financeService.getCategories });

  const {
    register,
    handleSubmit,
    reset,
    setError,
    clearErrors,
    setValue,
    formState: { errors },
  } = useForm<RecurringFormValues>({
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
    onMutate: () => {
      setFormApiError(null);
      clearErrors();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["recurring"] });
      reset();
      setEditingId(null);
    },
    onError: (error) => {
      const parsed = extractApiError(error, "Unable to save recurring transaction");
      setFormApiError(parsed.message);
      Object.entries(parsed.validationErrors).forEach(([field, message]) => {
        switch (field) {
          case "title":
          case "type":
          case "amount":
          case "categoryId":
          case "accountId":
          case "frequency":
          case "startDate":
          case "endDate":
          case "nextRunDate":
            setError(field, { type: "server", message });
            break;
          default:
            break;
        }
      });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => financeService.deleteRecurring(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ["recurring"] }),
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[0.95fr_1.25fr]">
        <Panel title={editingId ? "Edit Recurring Rule" : "Create Recurring Rule"} description="Automate salary, rent, subscriptions, and other repeated transactions.">
        <form
          className="grid gap-4"
          onSubmit={handleSubmit((values) => {
            clearErrors(["nextRunDate"]);
            setFormApiError(null);
            if (values.nextRunDate < values.startDate) {
              setError("nextRunDate", { type: "manual", message: "Next run date must be on or after start date" });
              return;
            }
            if (values.endDate && values.nextRunDate > values.endDate) {
              setError("nextRunDate", { type: "manual", message: "Next run date must be on or before end date" });
              return;
            }
            saveMutation.mutate(values);
          })}
        >
          <div>
            <label className="mb-2 block text-sm font-medium">Title</label>
            <input {...register("title", { required: "Title is required", minLength: { value: 2, message: "Title is too short" } })} />
            {errors.title ? <p className="mt-1 text-sm text-danger">{errors.title.message}</p> : null}
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Type</label>
              <select {...register("type", { required: "Type is required" })}>
                {transactionTypeOptions.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
              {errors.type ? <p className="mt-1 text-sm text-danger">{errors.type.message}</p> : null}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Amount</label>
              <input
                type="number"
                step="0.01"
                {...register("amount", {
                  valueAsNumber: true,
                  min: { value: 0.01, message: "Amount must be greater than zero" },
                })}
              />
              {errors.amount ? <p className="mt-1 text-sm text-danger">{errors.amount.message}</p> : null}
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">Account</label>
              <select {...register("accountId", { required: "Account is required" })}>
                <option value="">Select account</option>
                {(accountsQuery.data ?? []).map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name}
                  </option>
                ))}
              </select>
              {errors.accountId ? <p className="mt-1 text-sm text-danger">{errors.accountId.message}</p> : null}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Category</label>
              <select {...register("categoryId")}>
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
              <select {...register("frequency", { required: "Frequency is required" })}>
                {recurringFrequencyOptions.map((frequency) => (
                  <option key={frequency} value={frequency}>
                    {frequency}
                  </option>
                ))}
              </select>
              {errors.frequency ? <p className="mt-1 text-sm text-danger">{errors.frequency.message}</p> : null}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Start date</label>
              <input type="date" {...register("startDate", { required: "Start date is required" })} />
              {errors.startDate ? <p className="mt-1 text-sm text-danger">{errors.startDate.message}</p> : null}
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Next run date</label>
              <input type="date" {...register("nextRunDate", { required: "Next run date is required" })} />
              {errors.nextRunDate ? <p className="mt-1 text-sm text-danger">{errors.nextRunDate.message}</p> : null}
            </div>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="mb-2 block text-sm font-medium">End date</label>
              <input type="date" {...register("endDate")} />
              {errors.endDate ? <p className="mt-1 text-sm text-danger">{errors.endDate.message}</p> : null}
            </div>
            <label className="flex items-center gap-3 rounded-2xl bg-white/70 px-4 py-3">
              <input type="checkbox" className="h-5 w-5" {...register("autoCreateTransaction")} />
              <span className="text-sm font-medium text-ink">Automatically create transactions</span>
            </label>
          </div>
          {formApiError ? <p className="text-sm text-danger">{formApiError}</p> : null}
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
                  reset();
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
                    setValue("title", item.title);
                    setValue("type", item.type === "INCOME" ? "INCOME" : "EXPENSE");
                    setValue("amount", item.amount);
                    setValue("categoryId", item.categoryId ?? "");
                    setValue("accountId", item.accountId);
                    setValue("frequency", item.frequency);
                    setValue("startDate", item.startDate);
                    setValue("endDate", item.endDate ?? "");
                    setValue("nextRunDate", item.nextRunDate);
                    setValue("autoCreateTransaction", item.autoCreateTransaction);
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
