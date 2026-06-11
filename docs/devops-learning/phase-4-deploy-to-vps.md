# Phase 4 — Deploy to a VPS (registry, reverse proxy, TLS)

**Goal:** Take the locally-running stack and run it on a real, internet-facing server — hardened, pulling a prebuilt image from a registry, behind an Nginx reverse proxy with HTTPS.

**Status:** ✅ Complete

**Live at:** `https://urlshortener.nlqdat.io.vn`
**Server:** DigitalOcean droplet, Ubuntu 24.04, 1 vCPU / **512 MB RAM** (+1 GB swap), IP `139.59.106.91`.

---

## The end-to-end picture

```
Local machine                         VPS (Ubuntu, hardened)
─────────────                         ──────────────────────
1. docker compose build  ──push──►  GHCR (private image registry)
                                              │ pull (read-only PAT)
                                              ▼
browser ──HTTPS:443──►  Nginx (TLS termination, reverse proxy)
                            │ proxy_pass http://api:8080  (internal Docker network)
                            ▼
                         api ──► postgres   (migrator runs first, one-shot)
                            ▲
                         Let's Encrypt cert, auto-renewed via cron + certbot
```

Phase 4 = 5 stages: **A** provision + harden, **B** install Docker, **C** push image to a registry, **D** run the stack, **E** reverse proxy + TLS.

---

## What a VPS is (and why we chose one)

A **VPS** (Virtual Private Server) is a slice of a real server rented as your own Linux box: a public IP, root access over SSH, and nothing pre-installed. Unlike a platform (Heroku/Render), you build every layer yourself — which is exactly the point for learning. We used **DigitalOcean** for its beginner docs.

---

## Stage A — Provision & harden the server

### A.0 — SSH key authentication (concept)
You log into a VPS over **SSH**. Authenticate with a **key pair**, not a password:
- **Private key** (`id_ed25519`) — stays on your laptop, never shared.
- **Public key** (`id_ed25519.pub`) — placed on the server.

The server challenges you; only the private key can answer. There's no password for internet bots to brute-force.

```powershell
ssh-keygen -t ed25519 -C "your-email"
```
- `-t ed25519` — key **type**; ed25519 is modern, short, and secure (preferred over RSA).
- `-C "..."` — a **comment** label embedded in the public key (usually your email) to identify it.

View the public key to paste into DigitalOcean:
```powershell
Get-Content ~/.ssh/id_ed25519.pub
```

### A.1 — Create the droplet
Ubuntu 24.04 LTS, smallest plan, **SSH key auth** (paste the `.pub` contents — never the private key). Note the public IPv4.

First login:
```powershell
ssh root@139.59.106.91
```
First connect asks to trust the host fingerprint (`yes`) — it's then saved in `~/.ssh/known_hosts`.

### A.2 — Why harden a fresh box
A public IP on port 22 receives thousands of automated login attempts daily. Defense in depth:
1. Don't operate as root — use a normal user with `sudo` (speed bump + audit trail).
2. Key-only SSH, no root login — remove the two things bots attack (password guessing, the known `root` name).
3. Firewall — default-deny inbound except ports you serve.
4. Swap — on 512 MB RAM, an overflow file so memory spikes pause instead of OOM-killing containers.

> **🔑 Golden rule:** when changing SSH auth, keep your current session open and test every change in a **second** terminal. Never disable root/password until a new login as the new user has *succeeded separately*.

### A.3 — Update, swap, user, firewall (run as root)

```bash
apt update && apt upgrade -y
```
- `apt update` — refresh the package index (what's available).
- `apt upgrade -y` — install available upgrades; `-y` auto-confirms.

**Swap file (1 GB):**
```bash
fallocate -l 1G /swapfile      # create a 1 GB file; -l = length
chmod 600 /swapfile            # only root can read/write (swap must be private)
mkswap /swapfile               # format the file as swap space
swapon /swapfile               # enable it now
echo '/swapfile none swap sw 0 0' >> /etc/fstab   # persist across reboots
free -h                        # verify; -h = human-readable sizes
```
> **Trap:** without the `/etc/fstab` line, swap works now but **vanishes on reboot** ("works until the next reboot"). `fstab` is the file the kernel reads at boot to mount filesystems/swap.

**Non-root sudo user:**
```bash
adduser dat                    # creates user + home dir, prompts for a password
usermod -aG sudo dat           # add to the 'sudo' group; -a = append, -G = supplementary group
```
> `-a` is critical: `usermod -G sudo dat` **without** `-a` would *replace* all of dat's groups with just `sudo`. Always `-aG`.

**Give the user your SSH key:**
```bash
rsync --archive --chown=dat:dat ~/.ssh /home/dat
```
- `--archive` (`-a`) — recursive copy preserving permissions/timestamps.
- `--chown=dat:dat` — set owner:group of the copied files to `dat` (so dat owns its own `~/.ssh`).

**Firewall (order matters):**
```bash
ufw allow OpenSSH              # allow SSH FIRST, or enabling ufw locks you out
ufw enable                     # turn on the firewall (default deny inbound)
ufw status                     # confirm rules
```

**✅ Gate 1:** in a NEW terminal, `ssh dat@139.59.106.91`, then `sudo whoami` → `root`.

### A.4 — SSH lockdown (the lock-yourself-out step)

Config lives in `/etc/ssh/sshd_config`, **but** files in `/etc/ssh/sshd_config.d/*.conf` are included and **override** it (sorted by filename; later wins). DigitalOcean ships drop-ins there.

```bash
ls /etc/ssh/sshd_config.d/
sudo grep -rE 'PermitRootLogin|PasswordAuthentication' /etc/ssh/sshd_config /etc/ssh/sshd_config.d/
```
- `grep -r` — recurse into directories.
- `-E` — extended regex (so `|` means "or").

Ensure these are set (in the main file, or a drop-in that wins):
```
PermitRootLogin no
PasswordAuthentication no
```

**The `99-hardening.conf` convention:** rather than editing vendor files like `50-cloud-init.conf` (which the provider may **regenerate/overwrite** on image updates), create your own `/etc/ssh/sshd_config.d/99-hardening.conf`. The `99` prefix sorts last, so it **wins** over lower-numbered files. You keep editing that *same one file* over time (it never multiplies), and your intent stays isolated from provider-managed config.

Validate before applying, then restart:
```bash
sudo sshd -t                   # test config syntax; no output = valid
sudo systemctl restart ssh
```
> **Why `sshd -t` first:** sshd is **fail-closed** — a syntax error makes it refuse to start *entirely* (not partially). If you'd closed all sessions and restarted into a broken config, no new SSH connections are possible. `-t` catches that before you commit.

**✅ Gate 2** (all in NEW terminals, keep a working one open):
```powershell
ssh dat@139.59.106.91     # SUCCEEDS (key)
ssh root@139.59.106.91    # REFUSED (root login off)
ssh -o PreferredAuthentications=password -o PubkeyAuthentication=no dat@139.59.106.91  # REFUSED, no password prompt
```

### 🚑 If you lock yourself out of SSH
SSH is not your only way in. **DigitalOcean → your Droplet → "Console" (Recovery Console)** opens a browser-based terminal connected directly to the VM, **bypassing SSH/the network entirely**. Log in there (with the `dat` password you set), fix `/etc/ssh/sshd_config*`, run `sudo sshd -t`, `sudo systemctl restart ssh`. (Most cloud providers have an equivalent "web console"/"serial console".)

---

## Stage B — Install Docker

### Why the official repo / convenience script (and its caveats)
Ubuntu's bundled `docker.io` is often old. Docker's official install gives current Engine + the `docker compose` v2 plugin.

```bash
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
```
- `curl -fsSL`: `-f` fail on HTTP errors, `-s` silent, `-S` still show errors, `-L` follow redirects.
- `-o get-docker.sh` — save to a file.

> **"Fine for a single learning box":** the convenience script is `curl | sudo sh` (trusting a remote script as root), can't pin a version, and isn't idempotent — Docker says **don't** use it in production. There you'd add the apt repo manually with a pinned version (or via Ansible/cloud-init). For one throwaway VM it's fine.

**Run docker without sudo:**
```bash
sudo usermod -aG docker dat    # then log out + back in (group applies at login)
```
> **Security:** `docker` group ⇔ **root**. Anyone in it can `docker run -v /:/host ...` and own the whole filesystem. It is *not* "less than sudo" — only add people you'd give root. Not a security boundary.

**✅ Checks:** `docker version` (Client + Server), `docker compose version` (v2 plugin), `docker run --rm hello-world`.

---

## Stage C — Push the image to a registry (GHCR)

### Why a registry
The prod compose references `image: ghcr.io/...`. The server can't (and shouldn't) build from source, so the image must live somewhere both laptop and server reach. We used **GHCR** (`ghcr.io`) because it aligns with Phase 5 (GitHub Actions).

### Image naming
`registry/namespace/repository:tag` → `ghcr.io/ngoluuquocdat/url-shortener-api:latest`. Local images have no registry prefix, so you must **tag** them with the full path before pushing. A tag is just a *label pointing at an image ID*, not a copy — one image can have many names.

### Auth = Personal Access Token (not your password)
A **PAT** is a revocable token with explicit **scopes** (least privilege):
- Laptop (push): classic PAT, scope `write:packages` (implies `read:packages`).
- Server (pull only): classic PAT, scope `read:packages` only.

```powershell
$env:CR_PAT = "TOKEN"
$env:CR_PAT | docker login ghcr.io -u ngoluuquocdat --password-stdin
```
- `--password-stdin` — read the password from a pipe instead of typing it as an argument (keeps it out of shell history / process list).

```powershell
docker tag url-shortener-api:latest   ghcr.io/ngoluuquocdat/url-shortener-api:latest
docker tag url-shortener-api:migrator ghcr.io/ngoluuquocdat/url-shortener-api:migrator
docker push ghcr.io/ngoluuquocdat/url-shortener-api:latest
docker push ghcr.io/ngoluuquocdat/url-shortener-api:migrator
```
GHCR shows **one package** (`url-shortener-api`) with each tag as a **version** — one repository, many tags. That's normal.

On the **server**, log in with the read-only PAT so it can pull the **private** package:
```bash
echo "READ_ONLY_TOKEN" | docker login ghcr.io -u ngoluuquocdat --password-stdin
```

---

## Stage D — Run the stack on the server

### Why scp, not git
The prod box needs exactly 3 files: `docker-compose.yml`, `docker-compose.prod.yml`, `.env.prod` — not source code. `git clone` drags the whole repo (and `.env.prod` is gitignored anyway). `scp` copies exactly the named files over the existing SSH channel.

```bash
mkdir -p ~/app                 # -p: create parents as needed, no error if exists
```
```powershell
scp "UrlShortener.Api/docker-compose.yml"      dat@139.59.106.91:/home/dat/app/docker-compose.yml
scp "UrlShortener.Api/docker-compose.prod.yml" dat@139.59.106.91:/home/dat/app/docker-compose.prod.yml
scp "UrlShortener.Api/.env.prod"               dat@139.59.106.91:/home/dat/app/.env.prod
```
> **Windows scp traps learned:** quote paths containing spaces (the repo is under `Personal Project`); prefer **absolute** remote paths (`~` can be flaky); real OpenSSH scp prints a `Transferred:` summary — "1 file(s) copied" means a different command ran.

Verify on the server:
```bash
ls -la ~/app                   # -l long format (owner/size/time), -a show dotfiles (.env.prod is hidden!)
cat ~/app/.env.prod            # confirm the secret arrived intact
```

Launch:
```bash
cd ~/app
docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d
```
- `--env-file .env.prod` — supply prod values for `${...}` interpolation; **replaces** the auto-loaded `.env` (doesn't stack).
- `-f base -f prod` — naming files explicitly **disables** auto-loading `docker-compose.override.yml` (the dev file). This is how prod opts out of dev.
- `up -d` — detached/background (a server shouldn't tie the stack to your SSH session).

**✅ Checks:** `... ps` shows postgres healthy / migrator `Exited (0)` / api up; `curl http://localhost:8080/health` → Healthy. (At this stage the API was briefly published on `:8080`; Stage E removes that.)

---

## Stage E — Reverse proxy + TLS (Nginx + Let's Encrypt)

### Why a reverse proxy
Nginx sits in front: the browser connects to **it** over HTTPS (443); it terminates TLS and forwards plain HTTP to `api:8080` on the internal Docker network. One place handles certs; the app stops being directly exposed.

### The port puzzle (stop publishing 8080 in prod)
Compose merge is **additive** (can't remove). So: **base** has no `ports` for `api` (just `expose: ["8080"]`); **`override.yml`** *adds* `8080:8080` for dev. Prod inherits nothing → API unreachable from the host, only via Nginx. (Same trick used for Postgres' port.)

### E1 — Nginx as plain-HTTP proxy
Nginx is a prod concern → defined in `docker-compose.prod.yml`. Minimal `nginx.conf`:
```nginx
server {
    listen 80;
    server_name urlshortener.nlqdat.io.vn;
    location / {
        proxy_pass http://api:8080;             # resolves via Docker's internal DNS (service name)
        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;   # tells the app the original scheme
    }
}
```
Open the firewall (note caveat below): `sudo ufw allow 80` and `sudo ufw allow 443`.
**✅** `http://urlshortener.nlqdat.io.vn/health` → Healthy; `http://IP:8080` now refused.

### Why HTTPS needs a certificate
TLS does two jobs: **encryption** (anyone can self-sign — free) and **identity** (proving the server really is this domain). Without a cert signed by a trusted **CA**, a man-in-the-middle could offer their own encryption key — you'd be encrypted *to the attacker*. The cert is a CA's signed statement "this key belongs to this domain"; browsers trust CAs in advance, so they trust the cert. **The cert enables trust, not encryption.**

### E2a — Get the cert (HTTP-01 challenge via webroot)
Proof of control: place a token file Let's Encrypt names, served over your domain on port 80; LE fetches and **validates** it.

Three actors share files via **named volumes**:
- `certbot_webroot` — shared dropbox: certbot **writes** the token, nginx **reads/serves** it.
- `certbot_certs` — persistent `/etc/letsencrypt`; the issued cert must outlive the throwaway certbot container.

`certbot` service has **no command** because (a) the `certbot/certbot` image already has `ENTRYPOINT ["certbot"]`, and (b) it's an **on-demand** task run with *varying* subcommands (`certonly`, `renew`, `certificates`) — unlike the `migrator`, which runs the *same* command automatically on every `up` (so that one bakes its entrypoint in). Critically, certbot must **not** run on `up` (it would request a cert every start → rate limits).

Nginx must serve the challenge as a **file**, above the catch-all proxy:
```nginx
location /.well-known/acme-challenge/ { root /var/www/certbot; }
```

Request a **staging** cert first (untrusted, but generous rate limits — rehearse safely; production LE limits ~5 failures/hr, 50 certs/week):
```bash
docker compose ... run --rm certbot \
  certonly --webroot -w /var/www/certbot \
  -d urlshortener.nlqdat.io.vn \
  --email you@example.com --agree-tos --no-eff-email --staging
```
- `certonly` — obtain the cert only; don't auto-edit a web server (we drive Nginx ourselves).
- `--webroot -w DIR` — use the file-based challenge; `-w` is where to drop the token (the shared volume).
- `-d` — the domain to certify (cert is valid for this exact name).
- `--agree-tos --no-eff-email` — non-interactive acceptance; skip EFF mailing list.
- `--staging` — test CA; remove for the real cert.
- `run --rm` — run the one-shot service once and delete the container after (the cert survives in the volume).

### E2b — Real cert + HTTPS + redirect
1. Delete the staging lineage, re-request **without** `--staging`:
```bash
docker compose ... run --rm certbot delete --cert-name urlshortener.nlqdat.io.vn
docker compose ... run --rm certbot certonly --webroot -w /var/www/certbot -d urlshortener.nlqdat.io.vn --email you@example.com --agree-tos --no-eff-email
docker compose ... run --rm certbot certificates    # verify: no TEST_CERT marker
```
2. **App code change** (`Program.cs`): removed `app.UseHttpsRedirection()` (Nginx owns the redirect) and added `app.UseForwardedHeaders(...)` early, clearing `KnownNetworks`/`KnownProxies`. So the app sees the real scheme (`https`) and client IP from Nginx's `X-Forwarded-*` headers (correct redirect URLs, secure cookies, real IP). **Why clearing known-proxies is safe:** the API has no published port — only Nginx (internal network) can reach it — so no external client can forge those headers. Then rebuild → push → server `pull`.
3. **nginx.conf** → two server blocks: port 80 serves the ACME challenge and 301-redirects everything else to HTTPS; port 443 terminates TLS (`ssl_certificate` / `ssl_certificate_key` from `/etc/letsencrypt/live/<domain>/`) and proxies to `api:8080`. (The challenge location stays on 80, **not** redirected — renewals need it.)
4. **compose** → nginx mounts `certbot_certs:/etc/letsencrypt:ro` and `certbot_webroot:/var/www/certbot:ro` (read-only — nginx only reads; certbot writes), publishes `443:443`.

> **Port-mapping rule:** in `"443:443"` the **right** (container) side must equal Nginx's `listen` port; the left (host) side is free. `"443:123"` would fail unless Nginx listened on 123.

**✅** `https://urlshortener.nlqdat.io.vn/health` → Healthy with a **valid padlock**; `http://` → 301 to `https://`; app works over HTTPS; no redirect loop.

### E3 — Auto-renewal (certs last ~90 days)
`certbot renew` is a **no-op until ~30 days before expiry**, then renews via the same webroot challenge; Nginx must **reload** to pick up the new cert (it caches it in memory at start).

Test without waiting:
```bash
docker compose ... run --rm certbot renew --dry-run
```
Cron (root's crontab via `sudo crontab -e`), daily at 03:00, with logging:
```cron
0 3 * * * cd /home/dat/app && docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml run --rm certbot renew --quiet && docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml exec nginx nginx -s reload >> /var/log/cert-renew.log 2>&1
```
- Daily despite 90-day certs → many retry chances if a day fails (renew is a no-op until near expiry, so harmless).
- **Absolute path required:** cron sets `$HOME` to the crontab owner's home; in *root's* crontab `~` = `/root`, not `/home/dat`. Cron's minimal `PATH` is another reason to be explicit.
- `2>&1` — redirect stderr to the same place as stdout (the log).

Verify cron actually fires (without waiting): temporarily set `*/2 * * * *`, then
```bash
journalctl -u cron --since "10 min ago"   # shows cron LAUNCHED the job
sudo cat /var/log/cert-renew.log          # shows what the job DID
```
…then revert to `0 3 * * *`.

---

## Big gotchas learned in Phase 4

- **Docker bypasses UFW.** Published ports (`-p`) are inserted into iptables *before* UFW's filter chain, so UFW does **not** protect them. `https`/`8080` were reachable even without `ufw allow`. Lesson: for internal services, **"not published" (use `expose`, bind to `127.0.0.1`) is the real firewall — not UFW.** UFW still governs host listeners like SSH.
- **`~` in cron** = the crontab owner's `$HOME` (root's = `/root`). Always use absolute paths in cron.
- **sshd is fail-closed** — validate with `sshd -t` before restart; recover via the provider's web console.
- **Compose merge is additive** — express prod by *not adding* dev things in the base, not by trying to remove them.

## Artifacts
- Server: hardened Ubuntu droplet, Docker, `/home/dat/app/{docker-compose.yml, docker-compose.prod.yml, .env.prod, nginx.conf}`, cron renewal, `/var/log/cert-renew.log`.
- Repo: `nginx.conf`, updated `docker-compose.yml`/`override.yml`/`prod.yml`, `Program.cs` (`UseForwardedHeaders`), GHCR image `ghcr.io/ngoluuquocdat/url-shortener-api`.
