# Security Policy

Tunarrly is currently a single-user self-hosted MVP with optional built-in login. Do not expose it directly to the public internet without HTTPS and an authenticated reverse proxy or equivalent network access controls.

## Sensitive Data

- Lidarr API keys and AI provider tokens are stored in SQLite when saved through the UI.
- Lidarr API keys and AI provider tokens are encrypted in SQLite when `SECRETS__ENCRYPTIONKEY` is configured.
- Secrets are masked in the UI after save and are not intentionally logged.
- `.env` files and SQLite databases are ignored by Git.
- AI requests must not include local file paths, API keys, tokens, or raw filesystem structure.

## Music Library Safety

The music library mount must be read-only. The default Docker Compose mount is:

```yaml
- /path/to/music:/music:ro
```

Tunarrly must never rename, delete, modify, or write files in the music library.

## Reporting Vulnerabilities

Report security issues privately to the project maintainer. Do not disclose vulnerabilities publicly before a fix is available.
