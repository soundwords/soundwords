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
    }
}