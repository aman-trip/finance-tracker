import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { resetPasswordSchema, type ResetPasswordFormValues } from "../features/auth/schema";
import { financeService } from "../services/financeService";

export const ResetPasswordPage = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  const mutation = useMutation({
    mutationFn: async (values: ResetPasswordFormValues) => {
      if (!token) {
        throw new Error("Missing reset token");
      }
      const { data } = await financeService.resetPassword({ token, newPassword: values.newPassword });
      return data;
    },
  });

  useEffect(() => {
    if (!mutation.isSuccess) {
      return;
    }
    const timer = window.setTimeout(() => {
      navigate("/login");
    }, 1500);
    return () => window.clearTimeout(timer);
  }, [mutation.isSuccess, navigate]);

  return (
    <div className="auth-shell flex min-h-screen items-center justify-center px-4 py-8">
      <div className="auth-card w-full max-w-lg rounded-[34px] border border-line bg-panel p-7 shadow-panel sm:p-10">
        <div className="auth-badge">Account Recovery</div>
        <h1 className="mt-5 font-display text-4xl leading-tight text-ink sm:text-5xl">Reset password</h1>
        <p className="mt-4 max-w-md text-base leading-relaxed text-muted">
          Set a new password to continue using your account.
        </p>

        {!token ? (
          <div className="mt-8 space-y-4">
            <p className="text-sm text-danger">This reset link is invalid.</p>
            <Link to="/forgot-password" className="font-semibold text-accent transition hover:text-accent2">
              Request a new reset link
            </Link>
          </div>
        ) : (
          <form className="mt-8 space-y-5" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
            <div>
              <label className="mb-2 block text-sm font-semibold text-ink">New password</label>
              <input type="password" {...register("newPassword")} />
              {errors.newPassword ? <p className="mt-1 text-sm text-danger">{errors.newPassword.message}</p> : null}
            </div>
            <div>
              <label className="mb-2 block text-sm font-semibold text-ink">Confirm password</label>
              <input type="password" {...register("confirmPassword")} />
              {errors.confirmPassword ? <p className="mt-1 text-sm text-danger">{errors.confirmPassword.message}</p> : null}
            </div>

            {mutation.isSuccess ? <p className="text-sm text-success">Password reset successful. Redirecting to login...</p> : null}
            {mutation.isError ? <p className="text-sm text-danger">Reset failed. The link may be invalid or expired.</p> : null}

            <button
              type="submit"
              disabled={mutation.isPending || mutation.isSuccess}
              className="auth-cta w-full rounded-full bg-accent px-5 py-3 font-semibold text-white transition hover:brightness-110 disabled:opacity-60"
            >
              {mutation.isPending ? "Resetting..." : "Reset Password"}
            </button>
          </form>
        )}

        <p className="mt-7 text-sm text-muted">
          Back to{" "}
          <Link to="/login" className="font-semibold text-accent transition hover:text-accent2">
            Login
          </Link>
        </p>
      </div>
    </div>
  );
};
