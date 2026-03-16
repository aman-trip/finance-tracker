import { Link, NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

const navigation = [
  { to: "/", label: "Dashboard" },
  { to: "/transactions", label: "Transactions" },
  { to: "/accounts", label: "Accounts" },
  { to: "/budgets", label: "Budgets" },
  { to: "/goals", label: "Goals" },
  { to: "/reports", label: "Reports" },
  { to: "/recurring", label: "Recurring" },
];

export const Layout = () => {
  const { user, clearSession } = useAuth();

  return (
    <div className="min-h-screen bg-surface text-ink">
      <div className="mx-auto flex min-h-screen max-w-7xl flex-col px-4 py-6 sm:px-6 lg:px-8">
        <header className="mb-8 rounded-[28px] border border-line bg-panel/80 p-6 shadow-panel backdrop-blur">
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <Link to="/" className="font-display text-3xl font-semibold tracking-tight text-ink">
                Personal Finance Tracker
              </Link>
              <p className="mt-2 max-w-2xl text-sm text-muted">
                Track accounts, transactions, budgets, goals, recurring activity, and reporting in one place.
              </p>
            </div>
            <div className="flex flex-wrap items-center gap-3">
              <div className="rounded-full bg-white/70 px-4 py-2 text-sm text-muted">
                Signed in as <span className="font-semibold text-ink">{user?.displayName}</span>
              </div>
              <button
                type="button"
                onClick={clearSession}
                className="rounded-full bg-ink px-4 py-2 text-sm font-medium text-white transition hover:bg-slate-700"
              >
                Logout
              </button>
            </div>
          </div>
          <nav className="mt-6 flex flex-wrap gap-2">
            {navigation.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === "/"}
                className={({ isActive }) =>
                  `rounded-full px-4 py-2 text-sm font-medium transition ${
                    isActive ? "bg-accent text-white" : "bg-white/70 text-muted hover:bg-white"
                  }`
                }
              >
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
