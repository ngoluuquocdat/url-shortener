# Phase 2 — Full Local Compose (dev/prod split)

**Goal:** Run the whole stack locally with one command — API + Postgres + migrations — and cleanly separate development from production configuration.

**Status:** ✅ Complete

---

## What I built

Three Compose files in `UrlShortener.Api/`:

| File | Role | Loaded when |
|------|------|-------------|
| `docker-compose.yml` | **Base** — prod-shaped: images (not builds), no host ports for the DB, `Production` env, env-var creds with no defaults, healthcheck. | Always |
| `docker-compose.override.yml` | **Dev conveniences** — adds `build:` blocks, publishes `5464:5432`, weak dev creds, `Development` env. | Auto-loaded by `docker compose up` |
| `docker-compose.prod.yml` | **Prod extras** — adds `restart: unless-stopped`. | Only with explicit `-f` |

Plus a dedicated **`migrator`** service that applies EF Core migrations before the API starts.

## Key concepts learned

### Services and container networking
- `api`, `migrator`, and `postgres` are services on a Compose-created network.
- Containers reach each other by **service name**: the connection string uses `Host=postgres;Port=5432`, not `localhost`. DNS is provided by the Compose network.

### Configuration via environment variables
- .NET maps `ConnectionStrings__DefaultConnection` (env) → `ConnectionStrings:DefaultConnection` (config). The `__` becomes `:`.
- **Config precedence:** environment variables override `appsettings.json`. This is how the same image runs in dev and prod with different config.
- `ASPNETCORE_ENVIRONMENT` (runtime: Development/Production, controls Swagger etc.) is **different** from the build configuration (Debug/Release). Two separate axes.

### Migrations as a dedicated one-shot service (the key Phase 2 problem)
- Added a `migrator` **stage** in the Dockerfile (`FROM build`), installed `dotnet-ef` as a global tool, fixed `PATH` for `/root/.dotnet/tools`, and set `ENTRYPOINT dotnet ef database update`.
- Compose `migrator` service uses `build.target: migrator`.
- Ordering is enforced with `depends_on`:
  - `migrator` waits for `postgres` → `condition: service_healthy`.
  - `api` waits for `migrator` → `condition: service_completed_successfully`.
- **RUN (build-time) vs ENTRYPOINT (run-time):** tools installed with `RUN` bake into the image; the `ENTRYPOINT` is what executes when the container starts.
- A container runs **one PID 1 process** and exits when it finishes. The migrator runs, exits 0, and the API gate opens. Non-zero exit blocks the API.
- One-shot containers must have **no restart policy** — a clean exit (0) is not a "stop", so `restart: unless-stopped` would loop forever.

### The dev/prod split — and the limitation that shaped it
First attempt: a dev base + a prod override that tried to *remove* `build` and the published DB port. It didn't work, and `docker compose config` showed why:

> **Compose `-f` merge is purely additive. It can replace scalar values and add map keys, but it cannot REMOVE a key, a list entry, or a published port.** (`ports` merge by published+target+protocol and don't dedupe.)

The idiomatic fix — **neutral base + dev override**:
- Base describes what **production** runs.
- `docker-compose.override.yml` **adds** the dev-only conveniences (merge can always *add*).
- Naming files explicitly with `-f` **disables** auto-loading of `override.yml` — that's how prod opts out of dev.

```bash
docker compose config                                    # dev: base + override
docker compose -f docker-compose.yml -f docker-compose.prod.yml config   # prod: no override
```

### `build` + `image` coexistence
When a service has both, `build` produces a local image **tagged** with the `image` name (no registry involved). `docker compose up` uses an existing image if present, else falls back to building — which **fails on a prod host that has no source**. That's why prod must not inherit `build`.

### `ports` vs `expose`
- `ports:` publishes a container port to the **host** (`5464:5432`) — the only one that opens an external hole.
- `expose:` declares an **internal** port for container-to-container use.
- On a user-defined network, `expose` is **documentation, not a firewall** — containers can already reach all of each other's ports. It signals intent and is consumed by some proxy tooling.
- Postgres has `expose: ["5432"]` in the base (true in dev and prod); dev override *adds* the `5464:5432` host publish on top.

## Verification
- Both `docker compose config` renders inspected:
  - **Dev:** `build` + `image`, `5464:5432` published, dev creds, `Development`, `expose` + `ports`.
  - **Prod:** no `build`, no DB host port, `Production`, env-var creds with no defaults, `restart: unless-stopped`, `expose` only.
- Full stack verified on a fresh volume (`docker compose down -v` then up): migrations applied, API started.

## Known issues carried into Phase 3
- **Healthcheck/creds drift risk** — now both use `${POSTGRES_USER}` / `${POSTGRES_DB}` consistently (fixed at end of Phase 2).
- **Two sources of truth for DB creds** — postgres uses `POSTGRES_*`, while api/migrator use a separate `DB_CONNECTION_STRING`. Nothing forces them to agree → to be collapsed in Phase 3.

## Deferred to Phase 4
- `api` publishes `8080:8080` in the base. Once a reverse proxy is added, the API will stop publishing to the host and be reachable only on the internal network.

## Artifacts
- `UrlShortener.Api/docker-compose.yml`
- `UrlShortener.Api/docker-compose.override.yml`
- `UrlShortener.Api/docker-compose.prod.yml`
- `Dockerfile` (added `migrator` stage)
- `AppDbContextFactory.cs` (`.AddEnvironmentVariables()` + optional JSON)
