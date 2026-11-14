# BlueDoc Cloud Fleet API & Backend Services

Production-grade centralized fleet telemetry, machine enrollment, and alert management backend for **BlueDoc**.

## Technology Stack
- **Framework:** ASP.NET Core 8.0 Minimal APIs & Controllers
- **ORM:** Entity Framework Core 8.0 with PostgreSQL provider (Npgsql)
- **Database:** PostgreSQL 16
- **Authentication:** SHA-256 cryptographically hashed API Keys (Bearer / X-Api-Key)
- **Deployment:** Docker & Docker Compose

## Quick Start (Docker)

```bash
cd cloud
docker compose up -d --build
```

The service will be accessible at:
- **API Base:** `http://localhost:5000`
- **Swagger Documentation:** `http://localhost:5000/swagger`
- **Health Check:** `http://localhost:5000/health`

## Core Endpoints

| Method | Endpoint | Description | Auth Required? |
|---|---|---|---|
| `POST` | `/v1/auth/register-tenant` | Register organization, generate API key | No |
| `POST` | `/v1/auth/rotate-key` | Rotate existing API key | Yes |
| `POST` | `/v1/telemetry` | Ingest endpoint diagnostic snapshot | Yes |
| `GET` | `/v1/fleet/metrics` | Aggregated fleet KPIs and machines | Yes |
| `GET` | `/v1/fleet/machines` | Enrolled endpoint machine directory | Yes |
| `GET` | `/v1/alerts` | Active enterprise alerts | Yes |
| `POST` | `/v1/alerts/{id}/acknowledge`| Acknowledge alert | Yes |
| `GET` | `/health` | Service health status | No |

