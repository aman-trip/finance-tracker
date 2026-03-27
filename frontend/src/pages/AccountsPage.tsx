import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import { extractApiError } from "../utils/apiError";
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
  const [accountApiError, setAccountApiError] = useState<string | null>(null);
  const [transferApiError, setTransferApiError] = useState<string | null>(null);
  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });

  const {
    register: registerAccount,
    handleSubmit: handleAccountSubmit,
    reset: resetAccount,
    setError: setAccountFieldError,
    clearErrors: clearAccountErrors,
    formState: { errors: accountErrors },
  } = useForm<AccountFormValues>({
    defaultValues: {
      name: "",
      type: "BANK_ACCOUNT",
      openingBalance: 0,
      institutionName: "",
    },
  });

  const {
    register: registerTransfer,
    handleSubmit: handleTransferSubmit,
    reset: resetTransfer,
    setError: setTransferFieldError,
    clearErrors: clearTransferErrors,
    formState: { errors: transferErrors },
  } = useForm<TransferFormValues>({
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
    onMutate: () => {
      setAccountApiError(null);
      clearAccountErrors();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      resetAccount();
    },
    onError: (error) => {
      const parsed = extractApiError(error, "Unable to create account");
      setAccountApiError(parsed.message);
      Object.entries(parsed.validationErrors).forEach(([field, message]) => {
        switch (field) {
          case "name":
          case "type":
          case "openingBalance":
          case "institutionName":
            setAccountFieldError(field, { type: "server", message });
            break;
          default:
            break;
        }
      });
    },
  });

  const transferMutation = useMutation({
    mutationFn: (values: TransferFormValues) => financeService.transferAccounts(values),
    onMutate: () => {
      setTransferApiError(null);
      clearTransferErrors();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["accounts"] });
      queryClient.invalidateQueries({ queryKey: ["transactions"] });
      resetTransfer({
        sourceAccountId: "",
        targetAccountId: "",
        amount: 0,
        transactionDate: new Date().toISOString().slice(0, 10),
        note: "",
      });
    },
    onError: (error) => {
      const parsed = extractApiError(error, "Unable to transfer funds");
      setTransferApiError(parsed.message);
      Object.entries(parsed.validationErrors).forEach(([field, message]) => {
        switch (field) {
          case "sourceAccountId":
          case "targetAccountId":
          case "amount":
          case "transactionDate":
          case "note":
            setTransferFieldError(field, { type: "server", message });
            break;
          default:
            break;
        }
      });
    },
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[1fr_1.2fr]">
      <div className="space-y-6">
        <Panel title="Create Account" description="Track bank accounts, cards, wallets, and savings in one ledger.">
          <form className="grid gap-4" onSubmit={handleAccountSubmit((values) => createMutation.mutate(values))}>
            <div>
              <label className="mb-2 block text-sm font-medium">Name</label>
              <input {...registerAccount("name", { required: "Name is required", minLength: { value: 2, message: "Name is too short" } })} />
              {accountErrors.name ? <p className="mt-1 text-sm text-danger">{accountErrors.name.message}</p> : null}
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Type</label>
                <select {...registerAccount("type", { required: "Type is required" })}>
                  {accountTypeOptions.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
                {accountErrors.type ? <p className="mt-1 text-sm text-danger">{accountErrors.type.message}</p> : null}
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Opening Balance</label>
                <input
                  type="number"
                  step="0.01"
                  {...registerAccount("openingBalance", {
                    valueAsNumber: true,
                    min: { value: 0, message: "Opening balance cannot be negative" },
                  })}
                />
                {accountErrors.openingBalance ? <p className="mt-1 text-sm text-danger">{accountErrors.openingBalance.message}</p> : null}
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Institution</label>
              <input {...registerAccount("institutionName")} placeholder="Optional bank or issuer" />
            </div>
            {accountApiError ? <p className="text-sm text-danger">{accountApiError}</p> : null}
            <button type="submit" className="rounded-full bg-accent px-5 py-3 font-semibold text-white">
              {createMutation.isPending ? "Saving..." : "Create account"}
            </button>
          </form>
        </Panel>

        <Panel title="Transfer Between Accounts" description="Transfers create paired ledger entries and update both balances.">
          <form className="grid gap-4" onSubmit={handleTransferSubmit((values) => transferMutation.mutate(values))}>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Source account</label>
                <select {...registerTransfer("sourceAccountId", { required: "Source account is required" })}>
                  <option value="">Select source</option>
                  {(accountsQuery.data ?? []).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
                {transferErrors.sourceAccountId ? <p className="mt-1 text-sm text-danger">{transferErrors.sourceAccountId.message}</p> : null}
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Target account</label>
                <select {...registerTransfer("targetAccountId", { required: "Target account is required" })}>
                  <option value="">Select target</option>
                  {(accountsQuery.data ?? []).map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.name}
                    </option>
                  ))}
                </select>
                {transferErrors.targetAccountId ? <p className="mt-1 text-sm text-danger">{transferErrors.targetAccountId.message}</p> : null}
              </div>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <label className="mb-2 block text-sm font-medium">Amount</label>
                <input
                  type="number"
                  step="0.01"
                  {...registerTransfer("amount", {
                    valueAsNumber: true,
                    min: { value: 0.01, message: "Amount must be greater than zero" },
                  })}
                />
                {transferErrors.amount ? <p className="mt-1 text-sm text-danger">{transferErrors.amount.message}</p> : null}
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium">Date</label>
                <input type="date" {...registerTransfer("transactionDate", { required: "Date is required" })} />
                {transferErrors.transactionDate ? <p className="mt-1 text-sm text-danger">{transferErrors.transactionDate.message}</p> : null}
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium">Note</label>
              <input {...registerTransfer("note")} placeholder="Optional transfer note" />
            </div>
            {transferApiError ? <p className="text-sm text-danger">{transferApiError}</p> : null}
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
