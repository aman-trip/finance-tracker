import { api, publicApi } from "./api";
import type {
  Account,
  AccountBalanceTrendItem,
  AccountMember,
  Budget,
  Category,
  CategorySpendResponse,
  ForecastDaily,
  ForecastMonth,
  FutureBalancePrediction,
  Goal,
  HealthScore,
  IncomeExpenseTrendItem,
  Insight,
  InsightsOverview,
  MessageResponse,
  NetWorthResponse,
  PageResponse,
  RecurringTransaction,
  Rule,
  Transaction,
  TrendsResponse,
} from "../types";

export const financeService = {
  login: (payload: { email: string; password: string }) => publicApi.post("/api/auth/login", payload),
  register: (payload: { email: string; password: string; displayName: string }) => publicApi.post("/api/auth/register", payload),
  forgotPassword: (payload: { email: string }) => publicApi.post<MessageResponse>("/api/auth/forgot-password", payload),
  resetPassword: (payload: { token: string; newPassword: string }) => publicApi.post<MessageResponse>("/api/auth/reset-password", payload),

  getAccounts: async () => (await api.get<Account[]>("/api/accounts")).data,
  createAccount: async (payload: Record<string, unknown>) => (await api.post<Account>("/api/accounts", payload)).data,
  transferAccounts: async (payload: Record<string, unknown>) => api.post("/api/accounts/transfer", payload),

  getCategories: async () => (await api.get<Category[]>("/api/categories")).data,

  getTransactions: async (params: Record<string, unknown>) =>
    (await api.get<PageResponse<Transaction>>("/api/transactions", { params })).data,
  createTransaction: async (payload: Record<string, unknown>) => (await api.post<Transaction>("/api/transactions", payload)).data,
  updateTransaction: async (id: string, payload: Record<string, unknown>) =>
    (await api.put<Transaction>(`/api/transactions/${id}`, payload)).data,
  deleteTransaction: async (id: string) => api.delete(`/api/transactions/${id}`),

  getBudgets: async () => (await api.get<Budget[]>("/api/budgets")).data,
  createBudget: async (payload: Record<string, unknown>) => (await api.post<Budget>("/api/budgets", payload)).data,
  updateBudget: async (id: string, payload: Record<string, unknown>) => (await api.put<Budget>(`/api/budgets/${id}`, payload)).data,
  deleteBudget: async (id: string) => api.delete(`/api/budgets/${id}`),

  getGoals: async () => (await api.get<Goal[]>("/api/goals")).data,
  createGoal: async (payload: Record<string, unknown>) => (await api.post<Goal>("/api/goals", payload)).data,
  updateGoal: async (id: string, payload: Record<string, unknown>) => (await api.put<Goal>(`/api/goals/${id}`, payload)).data,
  contributeToGoal: async (id: string, payload: Record<string, unknown>) => (await api.post<Goal>(`/api/goals/${id}/contribute`, payload)).data,
  withdrawFromGoal: async (id: string, payload: Record<string, unknown>) => (await api.post<Goal>(`/api/goals/${id}/withdraw`, payload)).data,

  getRecurring: async () => (await api.get<RecurringTransaction[]>("/api/recurring")).data,
  createRecurring: async (payload: Record<string, unknown>) => (await api.post<RecurringTransaction>("/api/recurring", payload)).data,
  updateRecurring: async (id: string, payload: Record<string, unknown>) =>
    (await api.put<RecurringTransaction>(`/api/recurring/${id}`, payload)).data,
  deleteRecurring: async (id: string) => api.delete(`/api/recurring/${id}`),

  getForecastMonth: async () => (await api.get<ForecastMonth>("/api/forecast/month")).data,
  getForecastDaily: async () => (await api.get<ForecastDaily>("/api/forecast/daily")).data,
  getHealthScore: async () => (await api.get<HealthScore>("/api/insights/health-score")).data,
  getInsightsOverview: async () => (await api.get<InsightsOverview>("/api/insights")).data,
  getRules: async () => (await api.get<Rule[]>("/api/rules")).data,
  createRule: async (payload: Record<string, unknown>) => (await api.post<Rule>("/api/rules", payload)).data,
  updateRule: async (id: string, payload: Record<string, unknown>) => (await api.put<Rule>(`/api/rules/${id}`, payload)).data,
  deleteRule: async (id: string) => api.delete(`/api/rules/${id}`),
  getAccountMembers: async (id: string) => (await api.get<AccountMember[]>(`/api/accounts/${id}/members`)).data,
  inviteAccountMember: async (id: string, payload: Record<string, unknown>) =>
    (await api.post<AccountMember>(`/api/accounts/${id}/invite`, payload)).data,
  updateAccountMember: async (id: string, userId: string, payload: Record<string, unknown>) =>
    (await api.put<AccountMember>(`/api/accounts/${id}/members/${userId}`, payload)).data,

  getCategorySpend: async (params?: Record<string, unknown>) =>
    (await api.get<CategorySpendResponse>("/api/reports/category-spend", { params })).data,
  getIncomeExpenseTrend: async (params?: Record<string, unknown>) =>
    (await api.get<IncomeExpenseTrendItem[]>("/api/reports/income-vs-expense", { params })).data,
  getAccountBalanceTrend: async (params?: Record<string, unknown>) =>
    (await api.get<AccountBalanceTrendItem[]>("/api/reports/account-balance-trend", { params })).data,
  getReportTrends: async (params?: Record<string, unknown>) =>
    (await api.get<TrendsResponse>("/api/reports/trends", { params })).data,
  getNetWorth: async (params?: Record<string, unknown>) =>
    (await api.get<NetWorthResponse>("/api/reports/net-worth", { params })).data,
  getInsights: async () => (await api.get<Insight[]>("/api/reports/insights")).data,
  getFutureBalancePrediction: async () =>
    (await api.get<FutureBalancePrediction>("/api/reports/future-balance-prediction")).data,
};
