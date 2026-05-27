# Contributing to Tunarrly

Tunarrly is an early open-source MVP. Contributions should keep the app safe for self-hosted music libraries and preserve the separation between UI, scanning, Lidarr integration, AI integration, and recommendation logic.

## Development Setup

```bash
dotnet restore
dotnet test
dotnet build
dotnet run --project Tunarrly.Web
```

## Before Opening a PR

- Run `dotnet format`.
- Run `dotnet test`.
- Run `dotnet build`.
- Do not commit `.env` files, API keys, AI tokens, or SQLite databases.
- Do not add code that writes to the mounted music library.
- Keep AI optional and disabled by default.

## Architecture Rules

- Keep Lidarr API code isolated in infrastructure services.
- Keep AI provider code isolated in infrastructure services.
- Keep scanner and recommendation logic out of Razor components.
- Prefer explainable recommendation evidence over opaque scoring.
- Do not auto-add artists to Lidarr without user confirmation.
