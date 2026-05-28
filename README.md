# Tunarrly

**Tunarrly — AI-assisted music discovery for Lidarr, powered by your own library.**

Tunarrly is a self-hosted Lidarr companion that scans your mounted music library, syncs monitored artists from Lidarr, and recommends new artists using local evidence and optional OpenAI-compatible AI providers.

## MVP Status

This project is early MVP software. Run it on a trusted LAN or behind an authenticated reverse proxy. The MVP is single-user and does not include built-in authentication.

## What It Does

- Connects to Lidarr with a base URL and API key.
- Scans a read-only mounted music library.
- Indexes artists, albums, tracks, credits, Lidarr artists, recommendations, and AI runs in SQLite.
- Generates explainable local recommendations from artists already in your library, featured artists, collaborations, and shared genres.
- Optionally calls an OpenAI-compatible AI provider for second-stage recommendations.
- Requires user confirmation before adding any artist to Lidarr.

## Screenshots

Screenshots will be added after a tagged release. Current UI areas include Dashboard onboarding, Settings, Lidarr Connection, Library, Scan Jobs, Recommendations, Recommendation Detail, AI Provider, and AI Runs.

## First Run

1. Copy `.env.example` to `.env`.
2. Edit `LIDARR__BASEURL` and `LIDARR__APIKEY`.
3. Copy `docker-compose.example.yml` to `docker-compose.yml`.
4. Change the music mount in your local `docker-compose.yml` from `/path/to/music:/music:ro` to your library path.
5. Start Tunarrly with `docker compose up -d`.
6. Open `http://localhost:8080`.
7. Go to Settings and test Lidarr.
8. Load root folders and profiles from Lidarr.
9. Sync Lidarr artists.
10. Scan your library.
11. Generate local recommendations.
12. Enable AI only if you want optional second-stage AI recommendations.

## Built-In Login

Built-in single-user login is disabled by default. Enable it with environment variables:

```env
AUTH__ENABLED=true
AUTH__USERNAME=admin
AUTH__PASSWORD=change-this-password
```

When enabled, all app pages require login except `/login`, `/health`, `/ready`, and static assets. For internet-facing installs, prefer keeping Tunarrly behind a reverse proxy with HTTPS even when built-in login is enabled.

## Docker Compose

Copy `.env.example` to `.env`, copy `docker-compose.example.yml` to `docker-compose.yml`, edit local values, then run:

```bash
docker compose up -d
```

`docker-compose.yml` is intentionally ignored by Git so your local paths, ports, and networking choices are not overwritten by future pulls.

To run a local Docker smoke test without a real library, use:

```bash
bash scripts/docker-smoke.sh
```

The smoke test builds the image, starts the app, checks `/health` and `/ready`, verifies `/app/data` is writable, and verifies `/music` is read-only.

Default example compose service:

```yaml
volumes:
  - ./data:/app/data
  - /path/to/music:/music:ro
```

The music mount is read-only. Tunarrly must never write to `/music`.

## Container Image

The public image is published to GitHub Container Registry:

```bash
docker pull ghcr.io/darkatek7/tunarrly:latest
```

The example Compose file uses this image by default. For local development builds, replace the `image:` line with:

```yaml
services:
  tunarrly:
    build: .
```

Published tags include `latest`, `main`, tag names such as `v1.0.0`, and `sha-<commit>` tags.

## Required Lidarr Settings

Set these in `.env` or in the Settings page:

```env
LIDARR__BASEURL=http://lidarr:8686
LIDARR__APIKEY=change-me
LIDARR__DEFAULTROOTFOLDER=/music
LIDARR__DEFAULTQUALITYPROFILEID=1
LIDARR__DEFAULTMETADATAPROFILEID=1
LIDARR__DEFAULTMONITOR=all
LIDARR__SEARCHONADD=false
```

To find your Lidarr API key, open Lidarr and go to `Settings -> General -> Security -> API Key`.

If Tunarrly runs in Docker Compose with Lidarr on the same Docker network, `http://lidarr:8686` may work. If Lidarr runs on the host, use an address reachable from inside the Tunarrly container.

The Lidarr integration is covered by compatibility tests for expected API shapes. It has also been smoke-tested against a live Lidarr 3.1.0 instance for status, artists, profiles, root folders, lookup, and add-artist with album search disabled. Behavior can still vary by Lidarr version and configuration.

## Optional AI Provider

AI is disabled by default. Tunarrly supports providers that expose OpenAI-compatible `/chat/completions` endpoints.

Examples:

- OpenAI-compatible cloud APIs: `AI__BASEURL=https://api.openai.com/v1`
- Ollama: `AI__BASEURL=http://ollama:11434/v1`
- LM Studio: `AI__BASEURL=http://host.docker.internal:1234/v1`
- LocalAI: `AI__BASEURL=http://localai:8080/v1`
- vLLM: `AI__BASEURL=http://vllm:8000/v1`

The AI Provider page includes presets for these endpoints. Applying a preset does not overwrite a saved token.

Ollama example:

```env
AI__ENABLED=true
AI__BASEURL=http://ollama:11434/v1
AI__APIKEY=
AI__MODEL=llama3.1
```

LM Studio example:

```env
AI__ENABLED=true
AI__BASEURL=http://host.docker.internal:1234/v1
AI__APIKEY=
AI__MODEL=local-model
```

```env
AI__ENABLED=false
AI__BASEURL=https://api.openai.com/v1
AI__APIKEY=
AI__MODEL=gpt-4.1-mini
AI__TEMPERATURE=0.3
AI__TIMEOUTSECONDS=60
AI__MAXINPUTARTISTS=200
AI__MAXRECOMMENDATIONS=25
```

Tunarrly does not send local file paths or secrets to AI providers.

The AI Provider page can preview the exact sanitized payload before an AI recommendation run. Stored AI run outputs can be deleted from the AI Runs page.

## Development

```bash
dotnet restore
dotnet test
dotnet build
dotnet run --project Tunarrly.Web
```

Browser interaction tests use Playwright:

```bash
npm ci
npx playwright install chromium
npm run test:browser
```

The browser tests start an isolated local app instance, verify Blazor/MudBlazor assets, and exercise dashboard and settings interactions.

The default database path is `data/tunarrly.db` locally and `/app/data/tunarrly.db` in Docker.

## Health Checks

Tunarrly exposes non-sensitive health endpoints:

- `/health` returns app liveness.
- `/ready` checks SQLite connectivity.

The Docker image includes a minimal runtime healthcheck. Use `/ready` from your reverse proxy or external monitoring when HTTP-level readiness is needed.

## Privacy and Security

- Do not expose Tunarrly publicly without authentication in front of it.
- Built-in login is available via `AUTH__ENABLED=true` and env-seeded credentials.
- Lidarr API keys and AI provider tokens are encrypted in SQLite when `SECRETS__ENCRYPTIONKEY` is configured.
- Without `SECRETS__ENCRYPTIONKEY`, saved secrets fall back to plaintext local SQLite storage.
- Losing `SECRETS__ENCRYPTIONKEY` makes encrypted saved secrets unrecoverable; clear and re-enter them if the key is lost.
- Saved secrets are masked in the UI and are not intentionally logged.
- Use the Settings page to clear individual secrets or all saved secrets.
- AI is optional and disabled by default.
- Tunarrly does not send local file paths, API keys, tokens, or raw filesystem structure to AI providers.
- The music library should be mounted read-only with `:ro`.
- Scan progress and failed-file summaries show filenames only, not full local paths.

## Recommendation Sources

- Local recommendations are generated first and work without AI.
- AI recommendations are optional second-stage recommendations.
- Overlapping local and AI recommendations are merged and marked as Hybrid.
- Reasons and evidence are preserved so users can understand why an artist was recommended.

## Known Limitations

- MVP is single-user and has no built-in authentication.
- Real Lidarr add-artist behavior can vary by Lidarr version and profiles; test with your instance before relying on it.
- Scanner metadata quality depends on your tags.
- Background scans run in-process; cancelling running scans is planned but not complete.
- AI output quality depends entirely on the configured model/provider.
- Stored secrets are masked in the UI. Configure `SECRETS__ENCRYPTIONKEY` to encrypt saved secrets at rest.

## Troubleshooting

- If Docker Compose fails because `.env` is missing, copy `.env.example` to `.env` and edit it.
- If the app cannot write SQLite data, check permissions on `./data`.
- If Lidarr testing fails, verify `LIDARR__BASEURL` is reachable from the Tunarrly container and the API key is correct.
- If AI testing fails with a local provider, verify the provider exposes an OpenAI-compatible `/v1/chat/completions` endpoint.
- If scans find no tracks, verify the music volume is mounted to the same path configured in `LIBRARY__PATH`.
- If browser tests fail because port `5107` is busy, stop the process using that port and rerun `npm run test:browser`.

## Roadmap

- Stronger recommendation scoring and evidence drill-downs.
- Background queue and cancellation for long-running scans.
- More robust MusicBrainz/Lidarr matching.
- Authentication option for exposed deployments.
- More sample datasets and screenshots.
- Screenshots and release packaging after real-world testing.

## License

MIT
