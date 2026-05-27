# Tunarrly Agent Instructions

## Project summary
Tunarrly is a self-hosted AI-assisted music discovery companion for Lidarr. It is built with C#, .NET 10, Blazor Server/Blazor Web App, MudBlazor, SQLite, EF Core, Docker, and optional OpenAI-compatible AI providers.

## Core rules
- Never write to the mounted music library.
- Treat the music library as read-only.
- Never log Lidarr API keys or AI provider tokens.
- Do not send local file paths or secrets to AI providers.
- AI recommendations must be optional and disabled by default.
- Local recommendation logic must work without AI.
- Do not auto-add artists to Lidarr without user confirmation.
- Prefer explainable recommendations over black-box scoring.
- Keep UI logic out of Razor components where practical.
- Keep Lidarr API integration isolated.
- Keep AI provider integration isolated.
- Keep scanner, recommendation engine, and UI separated.

## Tech constraints
- Use .NET 10.
- Use MudBlazor for UI.
- Use SQLite with EF Core.
- Use Docker for deployment.
- Use OpenAI-compatible /chat/completions for AI.
- App should run on Linux containers.
- Persistent app data lives in /app/data.
- Music library is mounted at /music:ro by default.

## Validation before committing
Before every commit:
- Run dotnet format if available.
- Run dotnet build.
- Run tests if tests exist.
- Ensure no secrets are committed.
- Ensure .env is ignored.
- Ensure .env.example is safe.
- Ensure SQLite database files are ignored.
- Ensure Docker files still match README setup.

## Git rules
- Do not create a public GitHub repo until the app basics are working locally.
- First commit should include a buildable MVP skeleton, README, AGENTS.md, Dockerfile, docker-compose.yml, and .env.example.
- Use clear commit messages.
- Prefer small, meaningful commits after the initial baseline.

## Planning rule
For large changes, first write a short plan before editing code. For multi-step work, keep an implementation checklist in the Codex response or a temporary plan file if useful.
