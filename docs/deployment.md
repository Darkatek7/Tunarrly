# Deployment

Tunarrly is designed for self-hosted LAN deployments. For internet-facing use, put it behind HTTPS and authentication even when built-in login is enabled.

## Standalone Compose

1. Copy `.env.example` to `.env`.
2. Copy `docker-compose.example.yml` to `docker-compose.yml`.
3. Change the music mount to your library path and keep `:ro`.
4. Set `LIDARR__BASEURL` to a URL reachable from inside the container.
5. Set `AUTH__ENABLED=true` and choose a strong password.
6. Set `SECRETS__ENCRYPTIONKEY` to a long random value and keep it backed up.
7. Start with `docker compose up -d`.

Example service:

```yaml
services:
  tunarrly:
    image: ghcr.io/darkatek7/tunarrly:latest
    container_name: tunarrly
    ports:
      - "8080:8080"
    env_file:
      - .env
    volumes:
      - ./data:/app/data
      - /srv/music:/music:ro
    restart: unless-stopped
```

Open `http://localhost:8080` or `http://<server-ip>:8080`.

## Same Docker Network As Lidarr

If Lidarr already runs in Compose, attach Tunarrly to the same network and use the Lidarr service name.

```yaml
services:
  lidarr:
    image: lscr.io/linuxserver/lidarr:latest
    networks:
      - media

  tunarrly:
    image: ghcr.io/darkatek7/tunarrly:latest
    env_file:
      - .env
    volumes:
      - ./tunarrly-data:/app/data
      - /srv/music:/music:ro
    networks:
      - media
    restart: unless-stopped

networks:
  media:
    external: true
```

Set this in `.env`:

```env
LIDARR__BASEURL=http://lidarr:8686
```

The service name must match your actual Lidarr Compose service or network alias.

## Built-In Auth

Built-in login is single-user and seeded from environment variables:

```env
AUTH__ENABLED=true
AUTH__USERNAME=admin
AUTH__PASSWORD=change-this-password
```

Use a unique password. The password is read from configuration at startup; change it in `.env` and restart the container to rotate it.

Built-in auth protects app pages. `/health`, `/ready`, `/login`, and static assets remain public so health checks and the Blazor app can start correctly.

## Secret Encryption

Set `SECRETS__ENCRYPTIONKEY` before saving Lidarr or AI secrets in the UI:

```env
SECRETS__ENCRYPTIONKEY=replace-with-a-long-random-secret
```

When configured, saved secrets are encrypted in SQLite. Losing this value makes encrypted saved secrets unrecoverable; clear and re-enter them if the key is lost.

## Reverse Proxy Guidance

Use a reverse proxy for TLS, trusted hostnames, and optional extra authentication. Keep Tunarrly bound to an internal Docker network or LAN interface when possible.

### Caddy With Basic Auth

Generate a password hash with `caddy hash-password` and place it in the Caddyfile.

```caddyfile
tunarrly.example.com {
  encode zstd gzip

  basic_auth {
    admin <caddy-hashed-password>
  }

  reverse_proxy tunarrly:8080
}
```

### Traefik

This example assumes Traefik watches Docker labels and has an entrypoint named `websecure` and resolver named `letsencrypt`.

```yaml
services:
  tunarrly:
    image: ghcr.io/darkatek7/tunarrly:latest
    labels:
      - traefik.enable=true
      - traefik.http.routers.tunarrly.rule=Host(`tunarrly.example.com`)
      - traefik.http.routers.tunarrly.entrypoints=websecure
      - traefik.http.routers.tunarrly.tls.certresolver=letsencrypt
      - traefik.http.services.tunarrly.loadbalancer.server.port=8080
```

Add Traefik middleware for authentication or IP allowlisting if the route is public.

### Nginx

```nginx
server {
    listen 443 ssl http2;
    server_name tunarrly.example.com;

    ssl_certificate /etc/letsencrypt/live/tunarrly.example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/tunarrly.example.com/privkey.pem;

    location / {
        proxy_pass http://tunarrly:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

If you use Nginx basic auth, configure `auth_basic` and `auth_basic_user_file` inside the `server` or `location` block.

## Operational Checks

- `GET /health` verifies liveness.
- `GET /ready` verifies SQLite readiness.
- `docker logs tunarrly` should not contain API keys or AI tokens.
- The music mount must stay read-only: `/music:ro`.
- The data mount must be writable by the container user.
