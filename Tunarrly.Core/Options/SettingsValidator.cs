namespace Tunarrly.Core.Options;

public static class SettingsValidator
{
    public static IReadOnlyList<string> ValidateLidarr(LidarrOptions options, LibraryOptions library)
    {
        var errors = new List<string>();
        AddUrlError(errors, options.BaseUrl, "Lidarr base URL");
        if (string.IsNullOrWhiteSpace(options.DefaultRootFolder)) errors.Add("Default Lidarr root folder is required.");
        if (options.DefaultQualityProfileId <= 0) errors.Add("Default quality profile ID must be greater than 0.");
        if (options.DefaultMetadataProfileId <= 0) errors.Add("Default metadata profile ID must be greater than 0.");
        if (string.IsNullOrWhiteSpace(options.DefaultMonitor)) errors.Add("Default monitor behavior is required.");
        if (string.IsNullOrWhiteSpace(library.Path)) errors.Add("Music library path is required.");
        return errors;
    }

    public static IReadOnlyList<string> ValidateAi(AiOptions options)
    {
        var errors = new List<string>();
        if (options.Enabled)
        {
            AddUrlError(errors, options.BaseUrl, "AI provider base URL");
            if (string.IsNullOrWhiteSpace(options.Model)) errors.Add("AI model is required when AI is enabled.");
        }

        if (options.Temperature is < 0 or > 2) errors.Add("AI temperature must be between 0 and 2.");
        if (options.TimeoutSeconds < 5) errors.Add("AI timeout must be at least 5 seconds.");
        if (options.MaxInputArtists < 1) errors.Add("AI max input artists must be at least 1.");
        if (options.MaxRecommendations < 1) errors.Add("AI max recommendations must be at least 1.");
        return errors;
    }

    private static void AddUrlError(List<string> errors, string value, string label)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            errors.Add($"{label} must be a valid HTTP or HTTPS URL.");
        }
    }
}
