/** @type {import('tailwindcss').Config} */
export default {
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
        panel: "0 18px 40px rgba(20, 28, 34, 0.08)"
      },
      fontFamily: {
        display: ["Georgia", "serif"],
        body: ["Segoe UI", "sans-serif"]
      }
    },
  },
  plugins: [],
};
