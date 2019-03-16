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

        public SoundWordsConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration) : base(configuration)
        {
        }
    }
}