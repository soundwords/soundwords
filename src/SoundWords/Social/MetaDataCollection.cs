using System.Collections.Specialized;
using ServiceStack;

namespace SoundWords.Social
{
    internal class MetaDataCollection : NameValueCollection, IMetaDataCollection
    {
        public MetaDataCollection()
        {
            Add("og:type", "website");
            Add("og:site_name", "Sunne ord");
            Add("og:image", string.Format("{0}/images/metadata_logo_square.jpg", ServiceStackHost.Instance.Config.WebHostUrl));
            Add("fb:app_id", "144641815704745");
        }

        public string Title
        {
            get { return this["og:title"]; }
            set { this["og:title"] = value; }
        }

        public string Description
        {
            get { return this["og:description"]; }
            set { this["og:description"] = value; }
        }

        public string Url
        {
            get { return this["og:url"]; }
            set { this["og:url"] = value; }
        }
    }
}