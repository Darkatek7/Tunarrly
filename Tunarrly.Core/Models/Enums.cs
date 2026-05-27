namespace Tunarrly.Core.Models;

public static class CreditTypes
{
    public const string Main = "Main";
    public const string AlbumArtist = "AlbumArtist";
    public const string Featured = "Featured";
    public const string Composer = "Composer";
    public const string Other = "Other";
}

public static class RelationTypes
{
    public const string FeaturedOnTrack = "FeaturedOnTrack";
    public const string SharedGenre = "SharedGenre";
    public const string AlreadyInLibrary = "AlreadyInLibrary";
    public const string SimilarExternal = "SimilarExternal";
    public const string AiRecommended = "AIRecommended";
    public const string Manual = "Manual";
}

public static class RecommendationSources
{
    public const string Local = "Local";
    public const string Ai = "AI";
    public const string Hybrid = "Hybrid";
}

public static class RecommendationStatuses
{
    public const string New = "New";
    public const string Viewed = "Viewed";
    public const string Ignored = "Ignored";
    public const string MaybeLater = "MaybeLater";
    public const string AddedToLidarr = "AddedToLidarr";
    public const string FailedToAdd = "FailedToAdd";
}

public static class JobStatuses
{
    public const string Idle = "Idle";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
