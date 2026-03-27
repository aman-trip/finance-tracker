import { z } from "zod";

export const transactionSchema = z.object({
  accountId: z.string().uuid(),
  categoryId: z.string().optional(),
  type: z.enum(["INCOME", "EXPENSE"]),
  amount: z.coerce.number().positive(),
  transactionDate: z.string().min(1),
  merchant: z.string().optional(),
  note: z.string().optional(),
  paymentMethod: z.string().optional(),
});

export type TransactionFormValues = z.infer<typeof transactionSchema>;
