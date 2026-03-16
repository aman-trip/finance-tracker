import { api } from "./api";
import type {
  Account,
  AccountBalanceTrendItem,
  Budget,
  Category,
  CategorySpendResponse,
  Goal,
  IncomeExpenseTrendItem,
  PageResponse,
  RecurringTransaction,
  Transaction,
} from "../types";

export const financeService = {
  login: (payload: { email: string; password: string }) => api.post("/api/auth/login", payload),
  register: (payload: { email: string; password: string; displayName: string }) => api.post("/api/auth/register", payload),

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

  getCategorySpend: async (params?: Record<string, unknown>) =>
    (await api.get<CategorySpendResponse>("/api/reports/category-spend", { params })).data,
  getIncomeExpenseTrend: async (params?: Record<string, unknown>) =>
    (await api.get<IncomeExpenseTrendItem[]>("/api/reports/income-vs-expense", { params })).data,
  getAccountBalanceTrend: async (params?: Record<string, unknown>) =>
    (await api.get<AccountBalanceTrendItem[]>("/api/reports/account-balance-trend", { params })).data,
};
