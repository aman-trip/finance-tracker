import type { AccountType, GoalStatus, RecurringFrequency, TransactionType } from "../types";

export const accountTypeOptions: AccountType[] = [
  "BANK_ACCOUNT",
  "CREDIT_CARD",
  "CASH_WALLET",
  "SAVINGS_ACCOUNT",
];

export const transactionTypeOptions: TransactionType[] = ["INCOME", "EXPENSE"];

export const recurringFrequencyOptions: RecurringFrequency[] = ["DAILY", "WEEKLY", "MONTHLY", "YEARLY"];

export const goalStatusOptions: GoalStatus[] = ["ACTIVE", "COMPLETED", "CANCELLED"];
