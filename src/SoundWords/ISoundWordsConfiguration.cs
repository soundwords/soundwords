using System.Collections.Generic;
using ServiceStack.Configuration;

namespace SoundWords
{
    public interface ISoundWordsConfiguration : IAppSettings
    {
        List<string> AdminUserNames { get; }
        string RecordingsFolder { get; }
        string RestrictedRecordingsFolder { get; }
        string FacebookAppId { get; }
        string Protocol { get; }
        string SiteName { get; }
        bool DebugMode { get; }
        string CachePath { get; }
        string SiteUrl { get; }
        string CustomFolder { get; }
        string MetaDescription { get; }
        bool PiwikEnabled { get; }
        string PiwikDomains { get; }
        string PiwikHost { get; }
        string PiwikSiteId { get; }
        string LogoPath { get; }
        bool ShowLatestAlbums { get; }
        bool RecreateAuthTables { get; }
        string CompanyName { get; }
        string Slogan { get; }
        string CompanyEmail { get; }
        IList<string> PodcastCategories { get; }
        IList<string> PodcastSubcategories { get; }
    }
}