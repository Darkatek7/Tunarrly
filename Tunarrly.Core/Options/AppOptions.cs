namespace Tunarrly.Core.Options;

public sealed class LidarrOptions
{
    public string BaseUrl { get; set; } = "http://lidarr:8686";
    public string ApiKey { get; set; } = string.Empty;
    public string DefaultRootFolder { get; set; } = "/music";
    public int DefaultQualityProfileId { get; set; } = 1;
    public int DefaultMetadataProfileId { get; set; } = 1;
    public string DefaultMonitor { get; set; } = "all";
    public bool SearchOnAdd { get; set; }
}

public sealed class LibraryOptions
{
    public string Path { get; set; } = "/music";
}

public sealed class DatabaseOptions
{
    public string Path { get; set; } = "/app/data/tunarrly.db";
}

public sealed class AiOptions
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4.1-mini";
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxInputArtists { get; set; } = 200;
    public int MaxRecommendations { get; set; } = 25;
}

public sealed class AuthOptions
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
}

public sealed class SecretsOptions
{
    public string EncryptionKey { get; set; } = string.Empty;
}
