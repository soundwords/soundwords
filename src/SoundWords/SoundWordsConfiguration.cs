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
    public string RecordingsFolder => _configuration["RECORDINGS_FOLDER"] ?? "/var/audio/public";
    public string RestrictedRecordingsFolder => _configuration["RECORDINGS_FOLDER_RESTRICTED"] ?? "/var/audio/restricted";
    public string? FacebookAppId => _configuration["FACEBOOK_APP_ID"];
    public string Protocol => _configuration["PROTOCOL"] ?? "http";
    public string? SiteName => _configuration["SITE_NAME"];
    public bool DebugMode => _configuration.GetValue("DebugMode", false);
    public string CachePath => _configuration["CACHE_PATH"] ?? "/var/cache";
    public string? SiteUrl => _configuration["SITE_URL"];
    public string CustomFolder => _configuration["CUSTOM_FOLDER"] ?? "/var/custom";
    public string? MetaDescription => _configuration["META_DESCRIPTION"];
    public bool PiwikEnabled => _configuration.GetValue("PIWIK_ENABLED", false);
    public string? PiwikDomains => _configuration["PIWIK_DOMAINS"];
    public string? PiwikHost => _configuration["PIWIK_HOST"];
    public string? PiwikSiteId => _configuration["PIWIK_SITE_ID"];
    public string LogoPath => _configuration["LOGO_PATH"] ?? $"{SiteUrl}/content/images/logo.png";
    public bool ShowLatestAlbums => _configuration.GetValue("SHOW_LATEST_ALBUMS", true);
    public string? CompanyName => _configuration["COMPANY_NAME"];
    public string? Slogan => _configuration["SLOGAN"];
    public string? CompanyEmail => _configuration["COMPANY_EMAIL"];
    public IList<string> PodcastCategories => _configuration.GetSection("PODCAST_CATEGORIES").Get<List<string>>() ?? new List<string>();
    public IList<string> PodcastSubcategories => _configuration.GetSection("PODCAST_SUBCATEGORIES").Get<List<string>>() ?? new List<string>();
}
