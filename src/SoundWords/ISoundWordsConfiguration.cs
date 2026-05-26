namespace SoundWords;

public interface ISoundWordsConfiguration
{
    List<string> AdminUserNames { get; }
    string RecordingsFolder { get; }
    string RestrictedRecordingsFolder { get; }
    string? FacebookAppId { get; }
    string Protocol { get; }
    string? SiteName { get; }
    bool DebugMode { get; }
    string CachePath { get; }
    string? SiteUrl { get; }
    string CustomFolder { get; }
    string? MetaDescription { get; }
    bool PiwikEnabled { get; }
    string? PiwikDomains { get; }
    string? PiwikHost { get; }
    string? PiwikSiteId { get; }
    string LogoPath { get; }
    bool ShowLatestAlbums { get; }
    string? CompanyName { get; }
    string? Slogan { get; }
    string? CompanyEmail { get; }
    IList<string> PodcastCategories { get; }
    IList<string> PodcastSubcategories { get; }

    // --- Media (MinIO / S3-compatible) ---
    /// <summary>Base URL of the grey-cloud media host (e.g. <c>https://media.sunneord.no</c>). Defaults to <see cref="SiteUrl"/>.</summary>
    string MediaUrl { get; }

    /// <summary>S3 endpoint URL for signing (e.g. <c>http://minio:9000</c> inside the docker network).</summary>
    string? S3Endpoint { get; }

    string? S3AccessKey { get; }
    string? S3SecretKey { get; }

    /// <summary>Bucket name for anonymously-readable media (album art, public recordings).</summary>
    string S3PublicBucket { get; }

    /// <summary>Bucket name for restricted recordings (served only via pre-signed URLs).</summary>
    string S3RestrictedBucket { get; }

    /// <summary>How long a signed URL stays valid. Short for in-page playback; longer for personal podcast feeds.</summary>
    TimeSpan SignedUrlLifetime { get; }
}
