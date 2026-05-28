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

Screenshots will be added as the UI stabilizes.

## Docker Compose

Copy `.env.example` to `.env`, edit values, then run:

```bash
docker compose up --build
```

To run a local Docker smoke test without a real library, use:

```bash
bash scripts/docker-smoke.sh
```

The smoke test builds the image, starts the app, checks `/health` and `/ready`, verifies `/app/data` is writable, and verifies `/music` is read-only.

Default compose service:

```yaml
volumes:
  - ./data:/app/data
  - /path/to/music:/music:ro
```

The music mount is read-only. Tunarrly must never write to `/music`.

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

## Optional AI Provider

AI is disabled by default. Tunarrly supports providers that expose OpenAI-compatible `/chat/completions` endpoints.

Examples:

- OpenAI-compatible cloud APIs: `AI__BASEURL=https://api.openai.com/v1`
- Ollama: `AI__BASEURL=http://ollama:11434/v1`
- LM Studio: `AI__BASEURL=http://host.docker.internal:1234/v1`
- LocalAI: `AI__BASEURL=http://localai:8080/v1`
- vLLM: `AI__BASEURL=http://vllm:8000/v1`

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

## Development

```bash
dotnet restore
dotnet test
dotnet build
dotnet run --project Tunarrly.Web
```

The default database path is `data/tunarrly.db` locally and `/app/data/tunarrly.db` in Docker.

## Health Checks

Tunarrly exposes non-sensitive health endpoints:

- `/health` returns app liveness.
- `/ready` checks SQLite connectivity.

The Docker image includes a minimal runtime healthcheck. Use `/ready` from your reverse proxy or external monitoring when HTTP-level readiness is needed.

## Privacy and Security

- Do not expose Tunarrly publicly without authentication in front of it.
- Lidarr API keys and AI provider tokens are stored in SQLite when saved through the UI.
- Saved secrets are masked in the UI and are not intentionally logged.
- AI is optional and disabled by default.
- Tunarrly does not send local file paths, API keys, tokens, or raw filesystem structure to AI providers.
- The music library should be mounted read-only with `:ro`.

## Recommendation Sources

- Local recommendations are generated first and work without AI.
- AI recommendations are optional second-stage recommendations.
- Overlapping local and AI recommendations are merged and marked as Hybrid.
- Reasons and evidence are preserved so users can understand why an artist was recommended.

## Troubleshooting

- If Docker Compose fails because `.env` is missing, copy `.env.example` to `.env` and edit it.
- If the app cannot write SQLite data, check permissions on `./data`.
- If Lidarr testing fails, verify `LIDARR__BASEURL` is reachable from the Tunarrly container and the API key is correct.
- If AI testing fails with a local provider, verify the provider exposes an OpenAI-compatible `/v1/chat/completions` endpoint.
- If scans find no tracks, verify the music volume is mounted to the same path configured in `LIBRARY__PATH`.

## Roadmap

- Stronger recommendation scoring and evidence drill-downs.
- Background queue and cancellation for long-running scans.
- More robust MusicBrainz/Lidarr matching.
- Authentication option for exposed deployments.
- More tests and sample datasets.
- Screenshots and release packaging after real-world testing.

## License

MIT
