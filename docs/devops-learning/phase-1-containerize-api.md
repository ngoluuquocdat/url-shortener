# Phase 1 — Containerize the API

**Goal:** Package the .NET 8 API into a single, portable, production-shaped Docker image.

**Status:** ✅ Complete

---

## What I built

- A **multi-stage Dockerfile** for the API.
- A **`.dockerignore`** to keep the build context small and clean.
- Final image: **~229 MB**, runs as a **non-root** user, exposes **8080**.

## Key concepts learned

### Multi-stage builds
The Dockerfile uses two stages:
1. **Build stage** — `mcr.microsoft.com/dotnet/sdk:8.0`. Has the full SDK to `restore`, `build`, and `publish`.
2. **Runtime stage** — `mcr.microsoft.com/dotnet/aspnet:8.0`. Only the ASP.NET runtime, no SDK/compilers.

The published output is copied from build → runtime. The final image ships **only what's needed to run**, not the toolchain. This is why it's ~229 MB instead of ~700 MB+.

### Layer caching
Ordering matters. The Dockerfile copies **`.csproj` files first → `restore` → then copies the rest of the source → `publish`**. Because Docker caches layers by their inputs, dependency restore is only re-run when a `.csproj` changes — not on every source edit. Confirmed by seeing `CACHED` in the build output on a code-only change.

### Non-root + port 8080
- .NET 8 containers default to listening on **port 8080** (not 80) precisely so they can run as **non-root** (binding to ports < 1024 requires privileges).
- Added `USER app` so the process runs unprivileged. If the container is ever compromised, the attacker isn't root inside it.
- `EXPOSE 8080` documents the listening port.

### Container-root risk
Running as root inside a container is a real risk: container root can map to host root in some misconfigurations, and it widens the blast radius of any RCE. Non-root by default is the safe baseline.

### Deferred / noted for later
- **Distroless / chiseled images** (e.g. `dotnet/aspnet:8.0-jammy-chiseled`) — even smaller and lower attack surface. Deferred to keep Phase 1 simple.

## Verification
- `docker build` succeeded; image size confirmed ~229 MB.
- Re-build after a code-only change showed `CACHED` on the restore layer.

## Artifacts
- `Dockerfile` (repo root)
- `.dockerignore` (repo root)
