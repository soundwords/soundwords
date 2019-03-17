using ServiceStack;
using System.Collections.Generic;

namespace SoundWords
{
    internal class SoundWordsConfiguration : NetCoreAppSettings, ISoundWordsConfiguration
    {        
        public List<string> AdminUserNames => Get("ADMIN_USER_NAMES", new List<string>());

        public string RecordingsFolder => Get("RECORDINGS_FOLDER", "/var/audio/public");

        public string RestrictedRecordingsFolder => Get("RECORDINGS_FOLDER_RESTRICTED", "/var/audio/restricted");

        public string FacebookAppId => GetString("FACEBOOK_APP_ID");

        public string Protocol => Get("PROTOCOL", "http");

        public string SiteName => GetString("SITE_NAME");

        public bool DebugMode => Get("DebugMode", false);

        public string CachePath => Get("CACHE_PATH", "/var/cache");
        public string SiteUrl => GetString("SITE_URL");

        public string CustomFolder => Get("CUSTOM_FOLDER", "/var/custom");
        public string MetaDescription => GetString("META_DESCRIPTION");
        public bool PiwikEnabled => Get("PIWIK_ENABLED", false);
        public string PiwikDomains => GetString("PIWIK_DOMAINS");
        public string PiwikHost => GetString("PIWIK_HOST");
        public string PiwikSiteId => GetString("PIWIK_SITE_ID");
        public string LogoPath => Get("LOGO_PATH", $"{SiteUrl}/content/images/logo.png");
        public bool ShowLatestAlbums => Get("SHOW_LATEST_ALBUMS", true);
        public bool RecreateAuthTables => Get("RECREATE_AUTH_TABLES", false);
        public string CompanyName => GetString("COMPANY_NAME");
        public string Slogan => GetString("SLOGAN");
        public string CompanyEmail => GetString("COMPANY_EMAIL");
        public IList<string> PodcastCategories => GetList("PODCAST_CATEGORIES");
        public IList<string> PodcastSubcategories => GetList("PODCAST_SUBCATEGORIES");

        public SoundWordsConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration) : base(configuration)
        {
        }
    }
}