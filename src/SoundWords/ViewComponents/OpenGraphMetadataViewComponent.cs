using System.Collections.Generic;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;
using SoundWords.Social;
using SoundWords.Tools;

namespace SoundWords.ViewComponents
{
    public class OpenGraphMetadataViewComponent : ViewComponentBase
    {
        private readonly IMetaDataTool _metaDataTool;
        private readonly ISoundWordsConfiguration _soundWordsConfiguration;

        public OpenGraphMetadataViewComponent(IMetaDataTool metaDataTool, ISoundWordsConfiguration soundWordsConfiguration)
        {
            _metaDataTool = metaDataTool;
            _soundWordsConfiguration = soundWordsConfiguration;
        }

        public IViewComponentResult Invoke(OpenGraphMetadata data)
        {
            Metadata metaData = _metaDataTool.GetMetaData(new OpenGraphMetadata
                                                                            {
                                                                                Title = data.Title,
                                                                                Description = data.Description ?? _soundWordsConfiguration.MetaDescription,
                                                                                Url = data.Url ?? GetRequest().ToFullUrl(),
                                                                                SiteName = data.SiteName ?? _soundWordsConfiguration.SiteName,
                                                                                Type = data.Type ?? "website",
                                                                                AppId = data.AppId ?? _soundWordsConfiguration.FacebookAppId,
                                                                                Image = data.Image ??
                                                                                        $"{_soundWordsConfiguration.SiteUrl}/content/images/metadata_logo_square.jpg",
                                                                                Musician = data.Musician
                                                                            });

            return View(metaData);
        }
    }
}
