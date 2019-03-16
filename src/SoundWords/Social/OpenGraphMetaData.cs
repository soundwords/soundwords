using System.Runtime.Serialization;

namespace SoundWords.Social
{
    [DataContract]
    public class OpenGraphMetaData
    {
        private readonly ISoundWordsConfiguration _soundWordsConfiguration;

        public OpenGraphMetaData(ISoundWordsConfiguration soundWordsConfiguration)
        {
            _soundWordsConfiguration = soundWordsConfiguration;
            SiteName = soundWordsConfiguration.SiteName;
            Type = "website";
            AppId = soundWordsConfiguration.FacebookAppId;
            Image = string.Format("{0}images/metadata_logo_square.jpg", soundWordsConfiguration.SiteUrl);
        }

        [DataMember(Name = "og:title")]
        public string Title { get; set; }

        [DataMember(Name = "og:description")]
        public string Description { get; set; }

        [DataMember(Name = "og:url")]
        public string Url { get; set; }

        [DataMember(Name = "og:type")]
        public string Type { get; set; }

        [DataMember(Name = "og:site_name")]
        public string SiteName { get; set; }

        [DataMember(Name = "og:image")]
        public string Image { get; set; }

        [DataMember(Name = "fb:app_id")]
        public string AppId { get; set; }

        [DataMember(Name = "music:musician")]
        public string Musician { get; set; }
    }
}