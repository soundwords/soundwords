using Microsoft.Extensions.Configuration;

namespace SoundWords;

internal class SoundWordsConfiguration : ISoundWordsConfiguration
{
    private readonly IConfiguration _configuration;

    public SoundWordsConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<string> AdminUserNames => _configuration.GetSection("ADMIN_USER_NAMES").Get<List<string>>() ?? new List<string>();
    public string RecordingsFolder => Get("RECORDINGS_FOLDER") ?? "/var/audio/public";
    public string RestrictedRecordingsFolder => Get("RECORDINGS_FOLDER_RESTRICTED") ?? "/var/audio/restricted";
    public string? FacebookAppId => Get("FACEBOOK_APP_ID");
    public string Protocol => Get("PROTOCOL") ?? "http";
    public string? SiteName => Get("SITE_NAME");
    public bool DebugMode => _configuration.GetValue("DebugMode", false);
    public string CachePath => Get("CACHE_PATH") ?? "/var/cache";
    public string? SiteUrl => Get("SITE_URL");
    public string CustomFolder => Get("CUSTOM_FOLDER") ?? "/var/custom";
    public string? MetaDescription => Get("META_DESCRIPTION");
    public bool PiwikEnabled => _configuration.GetValue("PIWIK_ENABLED", false);
    public string? PiwikDomains => Get("PIWIK_DOMAINS");
    public string? PiwikHost => Get("PIWIK_HOST");
    public string? PiwikSiteId => Get("PIWIK_SITE_ID");
    public string LogoPath => Get("LOGO_PATH") ?? $"{SiteUrl}/content/images/logo.png";
    public bool ShowLatestAlbums => _configuration.GetValue("SHOW_LATEST_ALBUMS", true);
    public string? CompanyName => Get("COMPANY_NAME");
    public string? Slogan => Get("SLOGAN");
    public string? CompanyEmail => Get("COMPANY_EMAIL");
    public IList<string> PodcastCategories => _configuration.GetSection("PODCAST_CATEGORIES").Get<List<string>>() ?? new List<string>();
    public IList<string> PodcastSubcategories => _configuration.GetSection("PODCAST_SUBCATEGORIES").Get<List<string>>() ?? new List<string>();

    public string MediaUrl => Get("MEDIA_URL") ?? SiteUrl ?? string.Empty;
    public string? S3Endpoint => Get("S3_ENDPOINT");
    public string? S3AccessKey => Get("S3_ACCESS_KEY");
    public string? S3SecretKey => Get("S3_SECRET_KEY");
    public string S3PublicBucket => Get("S3_PUBLIC_BUCKET") ?? "public";
    public string S3RestrictedBucket => Get("S3_RESTRICTED_BUCKET") ?? "restricted";
    public TimeSpan SignedUrlLifetime => TimeSpan.FromMinutes(_configuration.GetValue("SIGNED_URL_LIFETIME_MINUTES", 120));

    /// <summary>
    /// Reads a configuration value, treating an empty string as missing.
    /// Needed because docker-compose's <c>${VAR-}</c> expansion injects
    /// <c>VAR=</c> when the variable is unset, which ASP.NET Core sees as
    /// the empty string — not <c>null</c> — so naive <c>??</c> fallbacks
    /// would never kick in.
    /// </summary>
    private string? Get(string key) =>
        _configuration[key] is { Length: > 0 } value ? value : null;
}
