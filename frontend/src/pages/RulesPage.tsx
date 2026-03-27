import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { extractApiError } from "../utils/apiError";
import { formatDate } from "../utils/format";

type RuleFormValues = {
  name: string;
  conditionJson: string;
  actionJson: string;
  isActive: boolean;
};

const defaultCondition = JSON.stringify(
  {
    merchantContains: "uber",
    typeEquals: "EXPENSE",
    minAmount: 10,
  },
  null,
  2,
);

const defaultAction = JSON.stringify(
  {
    setPaymentMethod: "CARD",
    appendNote: "#transport",
  },
  null,
  2,
);

export const RulesPage = () => {
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const [apiError, setApiError] = useState<string | null>(null);
  const rulesQuery = useQuery({ queryKey: ["rules"], queryFn: financeService.getRules });

  const { register, handleSubmit, reset, setValue, clearErrors, formState: { errors } } = useForm<RuleFormValues>({
    defaultValues: {
      name: "",
      conditionJson: defaultCondition,
      actionJson: defaultAction,
      isActive: true,
    },
  });

  const resetForm = () => {
    reset({
      name: "",
      conditionJson: defaultCondition,
      actionJson: defaultAction,
      isActive: true,
    });
    clearErrors();
    setEditingId(null);
    setApiError(null);
  };

  const validateJson = (value: string) => {
    try {
      JSON.parse(value.trim());
      return true;
    } catch {
      return "Invalid JSON format";
    }
  };

  const saveMutation = useMutation({
    mutationFn: (values: RuleFormValues) =>
      editingId ? financeService.updateRule(editingId, values) : financeService.createRule(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rules"] });
      resetForm();
    },
    onError: (error) => {
      setApiError(extractApiError(error, "Unable to save rule").message);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => financeService.deleteRule(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rules"] });
      if (editingId) {
        resetForm();
      }
    },
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_1.2fr]">
      <Panel title={editingId ? "Edit Rule" : "Create Rule"} description="Rules run on transaction creation and can classify or enrich entries automatically.">
        <form
          className="grid gap-4"
          onSubmit={handleSubmit((values) => {
            setApiError(null);
            saveMutation.mutate(values);
          })}
        >
          <div>
            <label className="mb-2 block text-sm font-medium">Rule name</label>
            <input {...register("name", { required: "Rule name is required" })} placeholder="Ride sharing auto-tag" />
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium">Condition JSON</label>
            <textarea
              rows={8}
              placeholder='{"merchantContains":"uber","typeEquals":"EXPENSE","minAmount":10}'
              {...register("conditionJson", {
                required: "Condition JSON is required",
                validate: validateJson,
                onChange: () => clearErrors("conditionJson"),
              })}
            />
            {errors.conditionJson ? (
              <p className="mt-1 text-xs text-danger">{errors.conditionJson.message}</p>
            ) : (
              <p className="mt-1 text-xs text-muted">Enter valid JSON. Example: {"{...}"}</p>
            )}
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium">Action JSON</label>
            <textarea
              rows={8}
              placeholder='{"setPaymentMethod":"CARD","appendNote":"#transport"}'
              {...register("actionJson", {
                required: "Action JSON is required",
                validate: validateJson,
                onChange: () => clearErrors("actionJson"),
              })}
            />
            {errors.actionJson ? (
              <p className="mt-1 text-xs text-danger">{errors.actionJson.message}</p>
            ) : (
              <p className="mt-1 text-xs text-muted">Enter valid JSON. Example: {"{...}"}</p>
            )}
          </div>
          <label className="inline-flex items-center gap-3 text-sm font-medium text-ink">
            <input type="checkbox" className="h-4 w-4 rounded border-line" {...register("isActive")} />
            Rule is active
          </label>
          {apiError ? <p className="text-sm text-danger">{apiError}</p> : null}
          <div className="flex gap-3">
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {saveMutation.isPending ? "Saving..." : editingId ? "Update rule" : "Create rule"}
            </button>
            <button type="button" className="rounded-full bg-white px-5 py-3 font-semibold text-ink" onClick={resetForm}>
              Clear
            </button>
          </div>
        </form>
      </Panel>

      <Panel title="Active Rules" description="Rules are stored as JSON so they stay flexible without changing existing transaction APIs.">
        <div className="space-y-4">
          {(rulesQuery.data ?? []).map((rule) => (
            <article key={rule.id} className="rounded-[24px] border border-line/70 bg-white/80 p-5 shadow-sm">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                  <h3 className="font-semibold text-ink">{rule.name}</h3>
                  <p className="mt-1 text-sm text-muted">Updated {formatDate(rule.updatedAt)}</p>
                </div>
                <div className="flex gap-2">
                  <span
                    className={`rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-[0.08em] ${
                      rule.isActive ? "border-success/25 bg-success/10 text-success" : "border-line/80 bg-white/80 text-muted"
                    }`}
                  >
                    {rule.isActive ? "Active" : "Inactive"}
                  </span>
                  <button
                    type="button"
                    className="rounded-full bg-white px-3 py-2 text-sm font-medium text-ink"
                    onClick={() => {
                      setEditingId(rule.id);
                      setValue("name", rule.name);
                      setValue("conditionJson", rule.conditionJson);
                      setValue("actionJson", rule.actionJson);
                      setValue("isActive", rule.isActive);
                    }}
                  >
                    Edit
                  </button>
                  <button
                    type="button"
                    className="rounded-full bg-danger px-3 py-2 text-sm font-medium text-white"
                    onClick={() => deleteMutation.mutate(rule.id)}
                  >
                    Delete
                  </button>
                </div>
              </div>
              <div className="mt-4 grid gap-4 md:grid-cols-2">
                <div className="rounded-2xl border border-line/70 bg-slate-950 p-4 text-xs text-slate-100">
                  <p className="mb-2 font-semibold uppercase tracking-[0.12em] text-slate-300">Condition</p>
                  <pre className="overflow-x-auto whitespace-pre-wrap">{rule.conditionJson}</pre>
                </div>
                <div className="rounded-2xl border border-line/70 bg-slate-950 p-4 text-xs text-slate-100">
                  <p className="mb-2 font-semibold uppercase tracking-[0.12em] text-slate-300">Action</p>
                  <pre className="overflow-x-auto whitespace-pre-wrap">{rule.actionJson}</pre>
                </div>
              </div>
            </article>
          ))}
          {!rulesQuery.data?.length ? <p className="text-sm text-muted">No rules configured yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
