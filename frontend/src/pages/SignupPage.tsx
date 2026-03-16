import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation } from "@tanstack/react-query";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router-dom";
import { financeService } from "../services/financeService";
import { useAuthStore } from "../store/authStore";
import type { AuthResponse } from "../types";
import { signupSchema, type SignupFormValues } from "../features/auth/schema";

export const SignupPage = () => {
  const navigate = useNavigate();
  const setSession = useAuthStore((state) => state.setSession);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<SignupFormValues>({
    resolver: zodResolver(signupSchema),
    defaultValues: { displayName: "", email: "", password: "", confirmPassword: "" },
  });

  const mutation = useMutation({
    mutationFn: async (values: SignupFormValues) =>
      (
        await financeService.register({
          displayName: values.displayName,
          email: values.email,
          password: values.password,
        })
      ).data as AuthResponse,
    onSuccess: (data) => {
      setSession({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
      navigate("/");
    },
  });

  return (
    <div className="flex min-h-screen items-center justify-center px-4">
      <div className="w-full max-w-lg rounded-[32px] border border-line bg-panel p-8 shadow-panel">
        <p className="text-sm uppercase tracking-[0.25em] text-muted">Start Fresh</p>
        <h1 className="mt-3 font-display text-4xl text-ink">Create your account</h1>
        <p className="mt-3 text-sm text-muted">Registration seeds your default income and expense categories automatically.</p>

        <form className="mt-8 grid gap-4 sm:grid-cols-2" onSubmit={handleSubmit((values) => mutation.mutate(values))}>
          <div className="sm:col-span-2">
            <label className="mb-2 block text-sm font-medium text-ink">Display name</label>
            <input {...register("displayName")} />
            {errors.displayName ? <p className="mt-1 text-sm text-danger">{errors.displayName.message}</p> : null}
          </div>
          <div className="sm:col-span-2">
            <label className="mb-2 block text-sm font-medium text-ink">Email</label>
            <input type="email" {...register("email")} />
            {errors.email ? <p className="mt-1 text-sm text-danger">{errors.email.message}</p> : null}
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium text-ink">Password</label>
            <input type="password" {...register("password")} />
            {errors.password ? <p className="mt-1 text-sm text-danger">{errors.password.message}</p> : null}
          </div>
          <div>
            <label className="mb-2 block text-sm font-medium text-ink">Confirm password</label>
            <input type="password" {...register("confirmPassword")} />
            {errors.confirmPassword ? <p className="mt-1 text-sm text-danger">{errors.confirmPassword.message}</p> : null}
          </div>
          <div className="sm:col-span-2">
            {mutation.isError ? <p className="mb-3 text-sm text-danger">Signup failed. The email may already be in use.</p> : null}
            <button
              type="submit"
              disabled={mutation.isPending}
              className="w-full rounded-full bg-accent px-5 py-3 font-semibold text-white transition hover:brightness-110 disabled:opacity-60"
            >
              {mutation.isPending ? "Creating account..." : "Sign up"}
            </button>
          </div>
        </form>

        <p className="mt-6 text-sm text-muted">
          Already registered?{" "}
          <Link to="/login" className="font-semibold text-accent">
            Login
          </Link>
        </p>
      </div>
    </div>
  );
};
