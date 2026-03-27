import { useEffect, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Panel } from "../components/Panel";
import { financeService } from "../services/financeService";
import type { AccountMembershipRole } from "../types";
import { extractApiError } from "../utils/apiError";
import { formatCurrency } from "../utils/format";

const membershipRoles: AccountMembershipRole[] = ["EDITOR", "VIEWER"];

export const SharedAccountsPage = () => {
  const queryClient = useQueryClient();
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState<AccountMembershipRole>("EDITOR");
  const [apiError, setApiError] = useState<string | null>(null);

  const accountsQuery = useQuery({ queryKey: ["accounts"], queryFn: financeService.getAccounts });
  const membersQuery = useQuery({
    queryKey: ["accounts", selectedAccountId, "members"],
    queryFn: () => financeService.getAccountMembers(selectedAccountId),
    enabled: Boolean(selectedAccountId),
  });

  useEffect(() => {
    if (!selectedAccountId && accountsQuery.data?.length) {
      setSelectedAccountId(accountsQuery.data[0].id);
    }
  }, [accountsQuery.data, selectedAccountId]);

  const inviteMutation = useMutation({
    mutationFn: () => financeService.inviteAccountMember(selectedAccountId, { email: inviteEmail, role: inviteRole }),
    onSuccess: () => {
      setInviteEmail("");
      setApiError(null);
      queryClient.invalidateQueries({ queryKey: ["accounts", selectedAccountId, "members"] });
    },
    onError: (error) => {
      setApiError(extractApiError(error, "Unable to invite member").message);
    },
  });

  const updateRoleMutation = useMutation({
    mutationFn: (payload: { userId: string; role: AccountMembershipRole }) =>
      financeService.updateAccountMember(selectedAccountId, payload.userId, { role: payload.role }),
    onSuccess: () => {
      setApiError(null);
      queryClient.invalidateQueries({ queryKey: ["accounts", selectedAccountId, "members"] });
    },
    onError: (error) => {
      setApiError(extractApiError(error, "Unable to update member role").message);
    },
  });

  return (
    <div className="grid gap-6 xl:grid-cols-[0.9fr_1.1fr]">
      <Panel title="Shared Accounts" description="Choose an account to inspect or manage family access.">
        <div className="space-y-3">
          {(accountsQuery.data ?? []).map((account) => (
            <button
              key={account.id}
              type="button"
              onClick={() => setSelectedAccountId(account.id)}
              className={`w-full rounded-[24px] border p-4 text-left shadow-sm transition-all duration-300 ${
                selectedAccountId === account.id
                  ? "border-accent bg-accent/10 text-ink"
                  : "border-line/70 bg-white/80 text-ink hover:-translate-y-0.5 hover:shadow-md"
              }`}
            >
              <p className="text-xs uppercase tracking-[0.18em] text-muted">{account.type.replace(/_/g, " ")}</p>
              <h3 className="mt-2 font-semibold">{account.name}</h3>
              <p className="mt-2 text-sm text-muted">{account.institutionName || "No institution linked"}</p>
              <p className="mt-3 font-semibold">{formatCurrency(account.currentBalance)}</p>
            </button>
          ))}
          {!accountsQuery.data?.length ? <p className="text-sm text-muted">No accounts available yet.</p> : null}
        </div>
      </Panel>

      <div className="space-y-6">
        <Panel title="Members" description="Owners can invite editors and viewers. Members can always inspect access lists.">
          <div className="space-y-4">
            {(membersQuery.data ?? []).map((member) => (
              <div key={member.userId} className="rounded-2xl border border-line/70 bg-white/80 p-4 shadow-sm">
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <p className="font-semibold text-ink">{member.displayName}</p>
                    <p className="text-sm text-muted">{member.email}</p>
                  </div>
                  <div className="flex items-center gap-3">
                    <span
                      className={`rounded-full border px-3 py-1 text-xs font-semibold uppercase tracking-[0.08em] ${
                        member.owner ? "border-accent/25 bg-accent/10 text-accent" : "border-line/80 bg-white/80 text-muted"
                      }`}
                    >
                      {member.role}
                    </span>
                    <select
                      value={member.role}
                      disabled={member.owner || updateRoleMutation.isPending}
                      onChange={(event) => updateRoleMutation.mutate({ userId: member.userId, role: event.target.value as AccountMembershipRole })}
                    >
                      {member.owner ? (
                        <option value="OWNER">OWNER</option>
                      ) : (
                        membershipRoles.map((role) => (
                          <option key={role} value={role}>
                            {role}
                          </option>
                        ))
                      )}
                    </select>
                  </div>
                </div>
              </div>
            ))}
            {!membersQuery.data?.length && selectedAccountId ? <p className="text-sm text-muted">No shared members on this account yet.</p> : null}
          </div>
        </Panel>

        <Panel title="Invite Member" description="Invite an existing user by email. Owner permissions are required for this action.">
          <div className="grid gap-4 md:grid-cols-[1.2fr_0.6fr_auto]">
            <input value={inviteEmail} onChange={(event) => setInviteEmail(event.target.value)} placeholder="person@example.com" />
            <select value={inviteRole} onChange={(event) => setInviteRole(event.target.value as AccountMembershipRole)}>
              {membershipRoles.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={!selectedAccountId || inviteMutation.isPending}
              className="rounded-full bg-accent px-5 py-3 font-semibold text-white disabled:opacity-50"
              onClick={() => inviteMutation.mutate()}
            >
              {inviteMutation.isPending ? "Inviting..." : "Invite"}
            </button>
          </div>
          {apiError ? <p className="mt-3 text-sm text-danger">{apiError}</p> : null}
        </Panel>
      </div>
    </div>
  );
};
