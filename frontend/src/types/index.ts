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

export type MessageResponse = {
  message: string;
};

export type AccountType = "BANK_ACCOUNT" | "CREDIT_CARD" | "CASH_WALLET" | "SAVINGS_ACCOUNT";
export type CategoryType = "INCOME" | "EXPENSE";
export type TransactionType = "INCOME" | "EXPENSE" | "TRANSFER_IN" | "TRANSFER_OUT";
export type GoalStatus = "ACTIVE" | "COMPLETED" | "CANCELLED";
export type RecurringFrequency = "DAILY" | "WEEKLY" | "MONTHLY" | "YEARLY";
export type AccountMembershipRole = "OWNER" | "EDITOR" | "VIEWER";

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

export type TrendPoint = {
  date: string;
  amount: number;
};

export type Insight = {
  type: string;
  title: string;
  description: string;
  severity: "HIGH" | "MEDIUM" | "LOW" | "INFO";
};

export type FutureBalancePrediction = {
  currentBalance: number;
  projectedRecurringNet: number;
  averageDailySpending: number;
  predictedBalance: number;
  horizonDays: number;
};

export type ForecastMonth = {
  currentBalance: number;
  forecastBalance: number;
  safeToSpend: number;
  averageDailySpend: number;
  recurringExpenses: number;
  risk: "LOW" | "MEDIUM" | "HIGH";
  horizonDays: number;
};

export type ForecastDailyPoint = {
  date: string;
  projectedBalance: number;
  safeToSpend: number;
  risk: "LOW" | "MEDIUM" | "HIGH";
};

export type ForecastDaily = {
  points: ForecastDailyPoint[];
  forecastBalance: number;
  safeToSpend: number;
  risk: "LOW" | "MEDIUM" | "HIGH";
};

export type HealthScoreBreakdownItem = {
  metric: string;
  score: number;
  maxScore: number;
  detail: string;
};

export type HealthScore = {
  score: number;
  breakdown: HealthScoreBreakdownItem[];
  suggestions: string[];
};

export type InsightsOverview = {
  healthScore: HealthScore;
  highlights: Insight[];
};

export type Rule = {
  id: string;
  name: string;
  conditionJson: string;
  actionJson: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type SavingsTrendPoint = {
  date: string;
  income: number;
  expense: number;
  savings: number;
};

export type CategoryTrendSeries = {
  category: string;
  points: TrendPoint[];
};

export type TrendsResponse = {
  incomeExpense: IncomeExpenseTrendItem[];
  savings: SavingsTrendPoint[];
  categoryTrends: CategoryTrendSeries[];
};

export type NetWorthPoint = {
  date: string;
  netWorth: number;
};

export type NetWorthResponse = {
  currentNetWorth: number;
  points: NetWorthPoint[];
};

export type AccountMember = {
  userId: string;
  email: string;
  displayName: string;
  role: AccountMembershipRole;
  owner: boolean;
};
