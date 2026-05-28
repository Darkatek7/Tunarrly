# Changelog

All notable changes to Tunarrly will be documented in this file.

## v0.1.2 - 2026-05-28

### Changed

- Redesigned the app shell, dashboard, login, settings, Lidarr, AI provider, library, scan jobs, artists, recommendations, AI runs, detail, error, and 404 pages with a darker media-console visual identity.
- Added shared page hero, panel, table, detail, notice, and empty-state styling for a more cohesive UI.

### Fixed

- Fixed authenticated deployments blocking fingerprinted static assets such as `app.*.css` before login.
- Added browser regression coverage to ensure the login shell receives the intended styled layout.

## v0.1.1 - 2026-05-28

### Fixed

- Fixed built-in login submissions being redirected back to `/login` before credentials were checked.
- Added browser coverage for the auth-enabled login flow and return URL handling.

## v0.1.0 - 2026-05-28

Initial MVP release.

### Added

- Blazor Server/Web App UI with MudBlazor pages for dashboard, settings, Lidarr connection, library, scan jobs, artists, recommendations, AI provider, and AI runs.
- SQLite persistence with EF Core migrations for indexed library metadata, Lidarr artists, recommendation runs, AI runs, settings, and scan jobs.
- Read-only music library scanner with background scan queue, progress tracking, skipped/failure summaries, idempotent rescans, and running-scan cancellation.
- Lidarr integration for status checks, artist sync, profile/root-folder loading, artist lookup, and user-confirmed add-artist requests.
- Explainable local recommendation engine with genre normalization, collaboration evidence, repeated-collaborator signals, and local/AI hybrid merging.
- Optional OpenAI-compatible AI recommendations with provider presets for cloud APIs, Ollama, LM Studio, LocalAI, and vLLM.
- Sanitized AI context preview that excludes local file paths, secrets, and raw filesystem structure.
- Built-in single-user cookie login seeded from environment variables.
- SQLite secret encryption for saved Lidarr and AI secrets when `SECRETS__ENCRYPTIONKEY` is configured.
- Dockerfile, Compose template, GHCR publishing workflow, build workflow, browser tests, and Docker smoke test.
- Deployment documentation for standalone Compose, same-network Lidarr, Caddy, Traefik, and Nginx.

### Security

- AI recommendations are disabled by default.
- Secrets are masked in the UI and can be cleared individually or all at once.
- Music library examples mount `/music` read-only.
- Scan progress and failed-file summaries show filenames only, not full local paths.

### Known Limitations

- Single-user MVP; built-in login is intentionally simple and should be paired with HTTPS/reverse-proxy access controls for exposed deployments.
- Recommendation quality depends on local tag quality and the configured AI model if AI is enabled.
- Background scans run in-process; queued job persistence is intentionally simple for the MVP.
