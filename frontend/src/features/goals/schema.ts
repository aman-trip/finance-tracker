import { z } from "zod";

export const goalSchema = z.object({
  name: z.string().min(2),
  targetAmount: z.coerce.number().positive(),
  targetDate: z.string().optional(),
  status: z.enum(["ACTIVE", "COMPLETED", "CANCELLED"]).default("ACTIVE"),
});

export const goalMovementSchema = z.object({
  goalId: z.string().uuid(),
  accountId: z.string().uuid(),
  amount: z.coerce.number().positive(),
  direction: z.enum(["contribute", "withdraw"]),
});

export type GoalFormValues = z.infer<typeof goalSchema>;
export type GoalMovementFormValues = z.infer<typeof goalMovementSchema>;
