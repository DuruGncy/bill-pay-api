# MobileProviderBillPaymentSystem

Lightweight billing API for a mobile provider — handles users, subscribers, bills and payments. Built on .NET 9 and intended to run locally, in Docker, or on Render.com behind an included YARP gateway.

Live deployments
- API: https://bill-pay-api.onrender.com
- Gateway (YARP): https://mobile-billing-gateway-2kqo.onrender.com

Overview
- API built with ASP.NET Core (.NET 9), EF Core (Postgres), JWT authentication, and API Versioning.
- Gateway is a separate YARP project that proxies traffic to the API; it can be used to expose multiple services or add routing policies.

Swagger / API Explorer
- API docs (Swagger UI) are available at:
  - https://bill-pay-api.onrender.com/swagger
  - When running locally: http://localhost:{port}/swagger
- API versioning is enabled; endpoints are grouped by version (e.g. `v1`).

Quick local start (CLI)
1. Restore & build
   - `dotnet restore`
   - `dotnet build`
2. Set local environment variables (example PowerShell)
   - $env:ConnectionStringsBillingDb="Host=localhost;Database=billing;Username=postgres;Password=secret"
   - $env:JwtKey="your-jwt-secret"
   - $env:JwtIssuer="https://your-issuer/"
   - $env:JwtAudience="your-audience"
3. Run
   - `dotnet run --project MobileProviderBillPaymentSystem`
4. Open Swagger: `http://localhost:{port}/swagger`

Running with Docker
- Build: `docker build -t billing-api .`
- Run (example):
  - `docker run -e ConnectionStringsBillingDb="Host=...;..." -e JwtKey="..." -p 5000:80 billing-api`
- Use `docker-compose` for multi-container setups (API + gateway + db).

Render.com deployment notes
- Render sets `RENDER_INTERNAL_HOSTNAME` for services in the same private network. The app detects that variable and uses a different connection-string key.
- Recommended environment variables to set in Render for the API service:
  - `ConnectionStringsBillingDb` — Postgres connection string (used when not running inside Render private network)
  - `ConnectionStringsBillingDbInternal` — Postgres connection string used when Render internal DNS/hostname is available (preferred when DB is in the same Render private network)
  - `JwtKey` — symmetric key used to sign JWTs
  - `JwtIssuer` — token issuer
  - `JwtAudience` — token audience
- If you deploy the gateway and API to the same private network on Render, set the API's internal connection string under `ConnectionStringsBillingDbInternal`.
- Start command: use your Dockerfile or set build & start commands per Render's docs; the app will run on the port Render exposes.

Gateway (YARP) on Render
- Gateway at https://mobile-billing-gateway-2kqo.onrender.com proxies client requests to the API service.
- When using the gateway, ensure gateway routing configuration (the `yarp.json` in the BillingGateway project) points to the API's internal hostname/URL used in Render.
- Use the gateway URL for public clients; it handles routing and can centralize cross-cutting concerns (auth, rate-limiting, logging).

Important configuration keys (exact names used by Program.cs)
- `ConnectionStringsBillingDb` (string) — Postgres connection string for non-Render or external DB
- `ConnectionStringsBillingDbInternal` (string) — Postgres connection string used when Render internal host is detected
- `JwtKey` (string) — symmetric signing key (UTF-8)
- `JwtIssuer` (string)
- `JwtAudience` (string)

Postgres connection string example
- Hosted/local: `Host=localhost;Port=5432;Database=billing;Username=postgres;Password=yourpassword;`
- Render-managed Postgres will provide a DB URL you must convert to Npgsql format or supply as the above.

Authentication
- JWT Bearer tokens are required for protected endpoints.
- Tokens are validated against `JwtKey`, `JwtIssuer`, and `JwtAudience`.
- In Swagger UI you can authenticate using the Bearer token input.

Database migrations
- If you need to apply EF Core migrations on the deployed DB:
  - Locally: `dotnet ef database update --project MobileProviderBillPaymentSystem`
  - On Render: run a one-off deploy hook or migration job that executes the update command against the production DB connection string.




