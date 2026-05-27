## Summary

- 

## Validation

- [ ] `dotnet format`
- [ ] `dotnet test`
- [ ] `dotnet build`
- [ ] Docker build tested if deployment files changed

## Safety Checklist

- [ ] Does not write to the mounted music library
- [ ] Does not log API keys or tokens
- [ ] Does not send local file paths or secrets to AI providers
- [ ] Does not auto-add artists to Lidarr without confirmation
