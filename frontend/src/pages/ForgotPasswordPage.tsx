import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link } from "react-router-dom";
import { forgotPasswordSchema, type ForgotPasswordFormValues } from "../features/auth/schema";
import { financeService } from "../services/financeService";
import { extractApiError } from "../utils/apiError";

const GENERIC_SUCCESS_MESSAGE = "If this email exists, a reset link has been sent";

export const ForgotPasswordPage = () => {
  const [apiError, setApiError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: "" },
  });

  const mutation = useMutation({
    mutationFn: async (values: ForgotPasswordFormValues) => {
      const { data } = await financeService.forgotPassword(values);
      return data;
    },
    onMutate: () => {
      setApiError(null);
    },
    onError: (error) => {
      setApiError(extractApiError(error, "Unable to send reset link. Please try again.").message);
    },
  });

  return (
    <div className="auth-shell flex min-h-screen items-center justify-center px-4 py-8">
      <div className="auth-card w-full max-w-lg rounded-[34px] border border-line bg-panel p-7 shadow-panel sm:p-10">
        <div className="auth-badge">Account Recovery</div>
        <h1 className="mt-5 font-display text-4xl leading-tight text-ink sm:text-5xl">Forgot password?</h1>
        <p className="mt-4 max-w-md text-base leading-relaxed text-muted">
          Enter your email and we&apos;ll generate a reset link for your account.
        </p>

        <form className="mt-8 space-y-5" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <div>
            <label className="mb-2 block text-sm font-semibold text-ink">Email</label>
            <input type="email" {...register("email")} />
            {errors.email ? <p className="mt-1 text-sm text-danger">{errors.email.message}</p> : null}
          </div>

          {mutation.isSuccess ? <p className="text-sm text-success">{GENERIC_SUCCESS_MESSAGE}</p> : null}
          {mutation.isError ? <p className="text-sm text-danger">{apiError ?? "Unable to send reset link. Please try again."}</p> : null}

          <button
            type="submit"
            disabled={mutation.isPending}
            className="auth-cta w-full rounded-full bg-accent px-5 py-3 font-semibold text-white transition hover:brightness-110 disabled:opacity-60"
          >
            {mutation.isPending ? "Sending..." : "Send Reset Link"}
          </button>
        </form>

        <p className="mt-7 text-sm text-muted">
          Remembered your password?{" "}
          <Link to="/login" className="font-semibold text-accent transition hover:text-accent2">
            Login
          </Link>
        </p>
      </div>
    </div>
  );
};
