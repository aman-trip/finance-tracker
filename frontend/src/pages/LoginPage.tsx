import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { financeService } from "../services/financeService";
import { useAuthStore } from "../store/authStore";
import type { AuthResponse } from "../types";
import { loginSchema, type LoginFormValues } from "../features/auth/schema";

export const LoginPage = () => {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "" },
  });

  const mutation = useMutation({
    mutationFn: async (values: LoginFormValues) => (await financeService.login(values)).data as AuthResponse,
    onSuccess: (data) => {
      setSession({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
      navigate("/");
    },
  });

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-md rounded-[32px] border border-line bg-panel p-8 shadow-panel">
        <p className="text-sm uppercase tracking-[0.25em] text-muted">Finance Tracker</p>
        <h1 className="mt-3 font-display text-4xl text-ink">Welcome back</h1>
        <p className="mt-3 text-sm text-muted">Sign in to manage your personal finances across accounts, budgets, goals, and reports.</p>

        <form className="mt-8 space-y-4" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <div>
            <label className="mb-2 block text-sm font-medium text-ink">Email</label>
            <input type="email" {...register("email")} />
            {errors.email ? <p className="mt-1 text-sm text-danger">{errors.email.message}</p> : null}
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium text-ink">Password</label>
            <input type="password" {...register("password")} />
            {errors.password ? <p className="mt-1 text-sm text-danger">{errors.password.message}</p> : null}
          </div>
          {mutation.isError ? <p className="text-sm text-danger">Login failed. Check your credentials and API availability.</p> : null}
          <button
            type="submit"
            disabled={mutation.isPending}
            className="w-full rounded-full bg-accent px-5 py-3 font-semibold text-white transition hover:brightness-110 disabled:opacity-60"
          >
            {mutation.isPending ? "Signing in..." : "Login"}
          </button>
        </form>

        <p className="mt-6 text-sm text-muted">
          No account yet?{" "}
          <Link to="/signup" className="font-semibold text-accent">
            Create one
          </Link>
        </p>
      </div>
    </div>
  );
};
