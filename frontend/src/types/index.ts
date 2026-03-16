export type User = {
  id: string;
  email: string;
  displayName: string;
};

export type AuthResponse = {
  accessToken: string;
  refreshToken: string;
  tokenType: string;
  expiresIn: number;
  user: User;
};

export type AccountType = "BANK_ACCOUNT" | "CREDIT_CARD" | "CASH_WALLET" | "SAVINGS_ACCOUNT";
export type CategoryType = "INCOME" | "EXPENSE";
export type TransactionType = "INCOME" | "EXPENSE" | "TRANSFER_IN" | "TRANSFER_OUT";
export type GoalStatus = "ACTIVE" | "COMPLETED" | "CANCELLED";
export type RecurringFrequency = "DAILY" | "WEEKLY" | "MONTHLY" | "YEARLY";

export type Account = {
  id: string;
  name: string;
  type: AccountType;
  openingBalance: number;
  currentBalance: number;
  institutionName: string | null;
  createdAt: string;
};

export type Category = {
  id: string;
  name: string;
  type: CategoryType;
  color: string;
  icon: string;
  archived: boolean;
};

export type Transaction = {
  id: string;
  accountId: string;
  accountName: string;
  categoryId: string | null;
  categoryName: string | null;
  type: TransactionType;
  amount: number;
  transactionDate: string;
  merchant: string | null;
  note: string | null;
  paymentMethod: string | null;
  createdAt: string;
  updatedAt: string;
};

export type Budget = {
  id: string;
  categoryId: string;
  categoryName: string;
  month: number;
  year: number;
  amount: number;
  alertThresholdPercent: number;
  spentAmount: number;
  utilizationPercent: number;
  alertLevel: string;
};

export type Goal = {
  id: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  targetDate: string | null;
  status: GoalStatus;
  progressPercent: number;
};

export type RecurringTransaction = {
  id: string;
  title: string;
  type: TransactionType;
  amount: number;
  categoryId: string | null;
  categoryName: string | null;
  accountId: string;
  accountName: string;
  frequency: RecurringFrequency;
  startDate: string;
  endDate: string | null;
  nextRunDate: string;
  autoCreateTransaction: boolean;
};

export type PageResponse<T> = {
  content: T[];
  totalElements: number;
  totalPages: number;
  number: number;
  size: number;
  first: boolean;
  last: boolean;
};

export type CategorySpendResponse = {
  items: { category: string; amount: number }[];
  total: number;
};

export type IncomeExpenseTrendItem = {
  date: string;
  income: number;
  expense: number;
};

export type AccountBalanceTrendItem = {
  accountName: string;
  points: { date: string; balance: number }[];
};
