import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig({
  define: {
    "import.meta.env.REACT_APP_API_BASE_URL": JSON.stringify(process.env.REACT_APP_API_BASE_URL ?? ""),
  },
  plugins: [react()],
  server: {
    port: 5173,
  },
});
