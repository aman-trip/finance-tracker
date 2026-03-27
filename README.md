# Personal Finance Tracker

React frontend with an ASP.NET Core Web API backend and PostgreSQL, packaged for Podman.

## Stack

- Backend: .NET 8, ASP.NET Core Web API, Entity Framework Core, Npgsql, JWT
- Frontend: React 18, Vite, Axios, Zustand, React Hook Form, Zod, Recharts, Tailwind CSS
- Runtime: Podman / `podman-compose`

## Project Structure

```text
finance-tracker/
  backend-dotnet/
    FinanceTracker.Api/
    FinanceTracker.Api.Tests/
  frontend/
  .env
  .env.example
  docker-compose.yml
  podman-compose.yml
  schema.sql
  README.md
```

## Removed Java Components

The Spring Boot backend has been removed from the repo:

- deleted `backend/` Java source tree
- deleted `backend/pom.xml`
- deleted Java backend Dockerfile and transitional Java compose wiring
- deleted `docker-compose.dotnet.yml` because the root compose files now run the .NET stack directly

## Environment

The backend reads these environment variables:

- `DB_HOST`
- `DB_PORT`
- `DB_NAME`
- `DB_USER`
- `DB_PASSWORD`
- `JWT_SECRET`
- `CORS_ALLOWED_ORIGINS`

The frontend build reads:

- `VITE_API_URL`

Defaults are provided in [.env.example](/mnt/e/GIT%20REPO/finance-tracker/.env.example) and a local runnable dev config is in [.env](/mnt/e/GIT%20REPO/finance-tracker/.env).

## Ports

- Frontend container: `http://localhost:4173`
- Frontend Vite dev server: `http://localhost:5173`
- Backend API: `http://localhost:8080`
- PostgreSQL: `localhost:5432`

The frontend still calls the backend on `http://localhost:8080`, so no React code changes are required. The default CORS configuration allows both the containerized frontend on `4173` and the local Vite dev server on `5173`.

## Podman Run

Start the full stack:

```bash
podman-compose up --build
```

Run detached:

```bash
podman-compose up --build -d
```

Stop and remove containers:

```bash
podman-compose down
```

Stop and remove containers plus database volume:

```bash
podman-compose down -v
```

## Direct Build Commands

Build the backend image directly:

```bash
podman build -t finance-tracker-backend ./backend-dotnet/FinanceTracker.Api
```

Build the frontend image directly:

```bash
podman build -t finance-tracker-frontend ./frontend
```

## Compose Services

- `postgres`: PostgreSQL 16 with schema initialization from [schema.sql](/mnt/e/GIT%20REPO/finance-tracker/schema.sql)
- `backend`: ASP.NET Core API built from [Dockerfile](/mnt/e/GIT%20REPO/finance-tracker/backend-dotnet/FinanceTracker.Api/Dockerfile)
- `frontend`: React app built with Vite and served by Nginx via [Dockerfile](/mnt/e/GIT%20REPO/finance-tracker/frontend/Dockerfile)

## API Compatibility

The .NET backend keeps the existing frontend contract:

- same routes under `/api/*`
- same JSON field names
- same JWT bearer header flow
- same PostgreSQL schema
- same default backend port `8080`

## Verification Commands

After startup:

```bash
podman-compose ps
podman-compose logs backend
podman-compose logs postgres
curl http://localhost:8080/actuator/health
```

The expected health response is:

```json
{"status":"UP"}
```

## Notes

- Both [docker-compose.yml](/mnt/e/GIT%20REPO/finance-tracker/docker-compose.yml) and [podman-compose.yml](/mnt/e/GIT%20REPO/finance-tracker/podman-compose.yml) now describe the same .NET-only stack.
- The frontend is served as static files through Nginx for a simpler production-style container.
- Replace the development `JWT_SECRET` in [.env](/mnt/e/GIT%20REPO/finance-tracker/.env) before using this outside local development.
