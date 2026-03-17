export type ThemeMode = "light" | "dark";

const THEME_STORAGE_KEY = "finance-theme";

const getSystemTheme = (): ThemeMode =>
  window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";

export const getStoredTheme = (): ThemeMode | null => {
  const raw = window.localStorage.getItem(THEME_STORAGE_KEY);
  if (raw === "light" || raw === "dark") {
    return raw;
  }
  return null;
};

export const resolveInitialTheme = (): ThemeMode => getStoredTheme() ?? getSystemTheme();

export const applyTheme = (theme: ThemeMode) => {
  document.documentElement.classList.toggle("dark", theme === "dark");
  document.documentElement.setAttribute("data-theme", theme);
};

export const persistTheme = (theme: ThemeMode) => {
  window.localStorage.setItem(THEME_STORAGE_KEY, theme);
};

