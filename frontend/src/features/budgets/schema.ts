import { z } from "zod";

export const budgetSchema = z.object({
  categoryId: z.string().uuid(),
  month: z.coerce.number().min(1).max(12),
  year: z.coerce.number().min(2000).max(2100),
  amount: z.coerce.number().positive(),
  alertThresholdPercent: z.coerce.number().min(1).max(200),
});

export type BudgetFormValues = z.infer<typeof budgetSchema>;
