/** @type {import('tailwindcss').Config} */
export default {
  darkMode: "class",
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        surface: "var(--surface)",
        panel: "var(--panel)",
        ink: "var(--ink)",
        muted: "var(--muted)",
        line: "var(--line)",
        accent: "var(--accent)",
        accent2: "var(--accent-2)",
        success: "var(--success)",
        danger: "var(--danger)"
      },
      boxShadow: {
        panel: "0 18px 40px rgba(15, 27, 45, 0.09)",
        lift: "0 14px 30px rgba(31, 94, 255, 0.14)"
      },
      fontFamily: {
        display: ["Plus Jakarta Sans", "Segoe UI", "sans-serif"],
        body: ["Segoe UI", "Inter", "sans-serif"]
      }
    },
  },
  plugins: [],
};
