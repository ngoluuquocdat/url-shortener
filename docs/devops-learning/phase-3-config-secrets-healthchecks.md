# Phase 3 — Config & Secrets + Health Checks

**Goal:** One source of truth for configuration, keep secrets out of git, and let the API report whether it can actually serve requests.

**Status:** ✅ Complete

---

## What I built

- A single `.env`-driven source of truth for DB credentials, with the connection string **assembled** from those values.
- `.env` / `.env.prod` kept out of git; a committed `.env.example` documents the contract.
- A **readiness health check** on `/health` that verifies the API can reach Postgres.

## Key concepts learned

### Two kinds of `${VAR}` — don't confuse them
- **`${VAR}` in the compose YAML** → resolved by the **Compose CLI at parse time**, from the shell or an **`.env`** file next to the compose file. This is *interpolation* — visible in `docker compose config`.
- **`environment:` / `env_file:` values** → handed **into the container** as its process environment; what the app reads at runtime.

The auto-read `.env` file feeds only the **first** kind.

### Single source of truth for credentials
Before: postgres read `POSTGRES_*`, while api/migrator read a separate fully-formed `DB_CONNECTION_STRING` — two places to keep in sync (drift bug).

After: `POSTGRES_*` is the only source. The connection string is **assembled** from them via interpolation in the base file:

```yaml
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
```

Consequence: `override.yml` no longer needs any creds — it collapsed to pure **dev structure** (`build:`, `5464:5432`, `Development`). Credentials became data in `.env`, not duplicated YAML.

### Managing dev vs prod config symmetrically
| | compose files | env file |
|---|---|---|
| **Dev** | base + `override.yml` (auto) | `.env` (auto) |
| **Prod** | `-f base -f prod.yml` (explicit) | `--env-file .env.prod` (explicit) |

- Compose auto-reads only **one** file named `.env`.
- **`--env-file .env.prod` REPLACES the default `.env`** (it doesn't stack) — so prod gets only prod values, no dev leakage. Symmetric to how naming `-f` files disables the auto-loaded `override.yml`.

### Secrets out of git
`.gitignore`:
```
.env
.env.*
!.env.example
```
- Ignore `.env` **and** the broad `.env.*` (so `.env.prod`, `.env.local`… are also ignored — a bare `.env` line would still leak `.env.prod`).
- Force-include `.env.example` with `!` so the *contract* (which keys exist) is shared without the secret *values*.
- Verified: `git check-ignore .env` → `.env`; `git check-ignore .env.example` → nothing.

### Health checks: liveness vs readiness
- **Liveness** — is the process alive? (`AddHealthChecks()` alone answers this.)
- **Readiness** — can it serve *right now*? (depends on DB etc.)

Chose **`AddDbContextCheck<AppDbContext>`** over `AspNetCore.HealthChecks.Npgsql`:
- **Decisive factor:** the Npgsql check needs a **separate connection string** — re-introducing the exact drift we just removed. `AddDbContextCheck` **reuses the registered `DbContext`**, so it pings with the *same* creds the app uses. Single source of truth preserved.
- It's lightweight — effectively `CanConnectAsync()` (a connection probe), not a heavy query.

Registered in Infrastructure DI (it's *about* `AppDbContext`, which lives there); the endpoint `MapHealthChecks("/health")` stays in the API layer.

```csharp
services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();
```

### Security: what a health endpoint should expose
- A **public**, unauthenticated `/health` should return only a **bare** `Healthy`/`Unhealthy` (200/503) — that's all an orchestrator or load balancer needs.
- A **detailed JSON** view (which dependency failed) is **reconnaissance** for an attacker and must be **auth-guarded**. (A detailed admin endpoint is parked as a future enhancement.)

## Verification
- `docker compose config` (dev) shows the assembled connection string matching the `.env` creds for both `api` and `migrator`.
- `git check-ignore` confirms `.env` ignored, `.env.example` tracked; git tool shows only `.env.example` in changes.
- `GET /health` → `Healthy` (200); `docker compose stop postgres` → `Unhealthy` (503); `start postgres` → recovers.

## Artifacts
- `UrlShortener.Api/.env` (ignored), `.env.prod` (ignored), `.env.example` (committed)
- `.gitignore` (env/secrets rules)
- `UrlShortener.Api/docker-compose.yml` (assembled connection string), `docker-compose.override.yml` (trimmed to dev structure)
- `UrlShortener.Infrastructure/DependencyInjectionSetup.cs` (`AddDbContextCheck`)
- `UrlShortener.Api/Program.cs` (`MapHealthChecks("/health")`)
