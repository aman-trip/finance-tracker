import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { accountTypeOptions } from "../utils/constants";
import { formatCurrency } from "../utils/format";

type AccountFormValues = {
  name: string;
  type: string;
  openingBalance: number;
  institutionName?: string;
};

type TransferFormValues = {
  sourceAccountId: string;
  targetAccountId: string;
  amount: number;
  transactionDate: string;
  note?: string;
};

export const AccountsPage = () => {
  const queryClient = useQueryClient();
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });

  const accountForm = useForm<AccountFormValues>({
    defaultValues: {
      name: "",
      type: "BANK_ACCOUNT",
      openingBalance: 0,
      institutionName: "",
    },
  });

  const transferForm = useForm<TransferFormValues>({
    defaultValues: {
      sourceAccountId: "",
      targetAccountId: "",
      amount: 0,
      transactionDate: new Date().toISOString().slice(0, 10),
      note: "",
    },
  });

  const createMutation = useMutation({
    mutationFn: (values: AccountFormValues) => financeService.createAccount(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      accountForm.reset();
    },
  });

  const transferMutation = useMutation({
    mutationFn: (values: TransferFormValues) => financeService.transferAccounts(values),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      transferForm.reset({
        sourceAccountId: "",
        targetAccountId: "",
        amount: 0,
        transactionDate: new Date().toISOString().slice(0, 10),
        note: "",
      });
    },
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_1.2fr]">
      <div className="space-y-6">
        <Panel title="Create Account" description="Track bank accounts, cards, wallets, and savings in one ledger.">
          <form className="grid gap-4" onSubmit={accountForm.handleSubmit((values) => createMutation.mutate(values))}>
            <div>
              <label className="mb-2 block text-sm font-medium">Name</label>
              <input {...accountForm.register("name", { required: true })} />
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Type</label>
                <select {...accountForm.register("type")}>
                  {accountTypeOptions.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Opening Balance</label>
                <input type="number" step="0.01" {...accountForm.register("openingBalance", { valueAsNumber: true })} />
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Institution</label>
              <input {...accountForm.register("institutionName")} placeholder="Optional bank or issuer" />
            </div>
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {createMutation.isPending ? "Saving..." : "Create account"}
            </button>
          </form>
        </Panel>

        <Panel title="Transfer Between Accounts" description="Transfers create paired ledger entries and update both balances.">
          <form className="grid gap-4" onSubmit={transferForm.handleSubmit((values) => transferMutation.mutate(values))}>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Source account</label>
                <select {...transferForm.register("sourceAccountId", { required: true })}>
                  <option value="">Select source</option>
                  {(accountsQuery.data ?? []).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Target account</label>
                <select {...transferForm.register("targetAccountId", { required: true })}>
                  <option value="">Select target</option>
                  {(accountsQuery.data ?? []).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Amount</label>
                <input type="number" step="0.01" {...transferForm.register("amount", { valueAsNumber: true })} />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Date</label>
                <input type="date" {...transferForm.register("transactionDate")} />
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Note</label>
              <input {...transferForm.register("note")} placeholder="Optional transfer note" />
            </div>
            <button type="submit" className="rounded-full bg-ink px-5 py-3 font-semibold text-white">
              {transferMutation.isPending ? "Transferring..." : "Transfer"}
            </button>
          </form>
        </Panel>
      </div>

      <Panel title="Accounts Overview" description="Live balances are recalculated through transaction and transfer activity.">
        <div className="grid gap-4 md:grid-cols-2">
          {(accountsQuery.data ?? []).map((account) => (
            <article key={account.id} className="rounded-[24px] bg-white/70 p-5">
              <p className="text-sm uppercase tracking-[0.18em] text-muted">{account.type.replace("_", " ")}</p>
              <h3 className="mt-2 font-display text-2xl text-ink">{account.name}</h3>
              <p className="mt-4 text-sm text-muted">Institution: {account.institutionName || "N/A"}</p>
              <p className="mt-2 text-sm text-muted">Opening balance: {formatCurrency(account.openingBalance)}</p>
              <p className="mt-4 text-2xl font-semibold text-ink">{formatCurrency(account.currentBalance)}</p>
            </article>
          ))}
          {!accountsQuery.data?.length ? <p className="text-sm text-muted">No accounts yet.</p> : null}
        </div>
      </Panel>
    </div>
  );
};
