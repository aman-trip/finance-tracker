import { Link, NavLink, Outlet } from "react-router-dom";
import { useEffect, useState } from "react";
import { useAuth } from "../hooks/useAuth";
import { applyTheme, persistTheme, resolveInitialTheme, type ThemeMode } from "../lib/theme";

type IconProps = { className?: string };

const DashboardIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M3 13h8V3H3v10Zm10 8h8V11h-8v10ZM3 21h8v-6H3v6Zm10-10h8V3h-8v8Z" />
  </svg>
);

const ListIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M9 6h11M9 12h11M9 18h11M4 6h.01M4 12h.01M4 18h.01" />
  </svg>
);

const WalletIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M3 7a2 2 0 0 1 2-2h13a3 3 0 0 1 0 6H5a2 2 0 0 1-2-2V7Z" />
    <path d="M3 9v8a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-6" />
    <circle cx="17.5" cy="14" r="1.2" />
  </svg>
);

const BudgetIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M4 5h16M6 9h12M4 13h16M6 17h12M4 21h16" />
  </svg>
);

const TargetIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <circle cx="12" cy="12" r="8" />
    <circle cx="12" cy="12" r="4" />
    <circle cx="12" cy="12" r="1.2" fill="currentColor" />
  </svg>
);

const ChartIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M4 19h16M7 15v-4m5 4V7m5 8v-6" />
  </svg>
);

const RepeatIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M17 1l4 4-4 4M3 11V9a4 4 0 0 1 4-4h14M7 23l-4-4 4-4m14-2v2a4 4 0 0 1-4 4H3" />
  </svg>
);

const SunIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <circle cx="12" cy="12" r="4.2" />
    <path d="M12 2v2.2M12 19.8V22M4.9 4.9l1.6 1.6M17.5 17.5l1.6 1.6M2 12h2.2M19.8 12H22M4.9 19.1l1.6-1.6M17.5 6.5l1.6-1.6" />
  </svg>
);

const MoonIcon = ({ className = "h-4 w-4" }: IconProps) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className={className}>
    <path d="M20 14.1A8.5 8.5 0 1 1 9.9 4a6.7 6.7 0 0 0 10.1 10.1Z" />
  </svg>
);

const navigation = [
  { to: "/", label: "Dashboard", icon: DashboardIcon },
  { to: "/transactions", label: "Transactions", icon: ListIcon },
  { to: "/accounts", label: "Accounts", icon: WalletIcon },
  { to: "/budgets", label: "Budgets", icon: BudgetIcon },
  { to: "/goals", label: "Goals", icon: TargetIcon },
  { to: "/reports", label: "Reports", icon: ChartIcon },
  { to: "/recurring", label: "Recurring", icon: RepeatIcon },
];

export const Layout = () => {
  const { user, clearSession } = useAuth();
  const [theme, setTheme] = useState<ThemeMode>(() => resolveInitialTheme());

  useEffect(() => {
    applyTheme(theme);
    persistTheme(theme);
  }, [theme]);

  const toggleTheme = () => setTheme((current) => (current === "dark" ? "light" : "dark"));

  return (
    <div className="min-h-screen bg-surface text-ink transition-colors">
      <div className="mx-auto flex min-h-screen max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
        <header className="mb-8 rounded-[28px] border border-line/80 bg-panel/95 p-6 shadow-panel backdrop-blur-xl transition-all duration-300">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <p className="text-xs uppercase tracking-[0.24em] text-accent/80">Finance Tracker</p>
              <Link
                to="/"
                className="mt-1 inline-block font-display text-4xl font-semibold tracking-tight text-ink transition-colors lg:text-5xl"
              >
                Financial Command Center
              </Link>
              <p className="mt-2 max-w-2xl text-sm leading-6 text-muted/85">
                Track accounts, transactions, budgets, goals, recurring activity, and reporting in one place.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <button
                type="button"
                onClick={toggleTheme}
                className="inline-flex items-center gap-2 rounded-full border border-line/80 bg-white px-4 py-2 text-sm font-medium text-muted shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:shadow-md hover:text-ink"
              >
                {theme === "dark" ? <SunIcon /> : <MoonIcon />}
                {theme === "dark" ? "Light mode" : "Dark mode"}
              </button>
              <div className="rounded-full border border-line/80 bg-white px-4 py-2 text-sm text-muted shadow-sm transition-all duration-300">
                Signed in as <span className="font-semibold text-ink">{user?.displayName}</span>
              </div>
              <button
                type="button"
                onClick={clearSession}
                className="rounded-full bg-ink px-4 py-2 text-sm font-medium text-white shadow-sm transition-all duration-300 hover:-translate-y-0.5 hover:bg-slate-800 hover:shadow-lg dark:hover:bg-slate-600"
              >
                Logout
              </button>
            </div>
          </div>
          <nav className="mt-6 flex flex-wrap gap-2 rounded-2xl border border-line/70 bg-white/60 p-2 transition-all duration-300">
            {navigation.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  `group inline-flex items-center gap-2 rounded-xl px-3.5 py-2.5 text-sm font-medium transition-all duration-300 ${
                    isActive
                      ? "scale-[1.03] bg-accent text-white shadow-lift"
                      : "text-muted hover:-translate-y-0.5 hover:bg-white/90 hover:text-ink hover:shadow-sm dark:hover:bg-white/80"
                  }`
                }
              >
                <item.icon className="h-4 w-4 transition-transform duration-300 group-hover:scale-110" />
                {item.label}
              </NavLink>
            ))}
          </nav>
        </header>
        <main className="flex-1">
          <Outlet />
        </main>
      </div>
    </div>
  );
};
