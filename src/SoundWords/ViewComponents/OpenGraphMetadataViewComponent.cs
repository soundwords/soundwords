using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using SoundWords.Social;

namespace SoundWords.ViewComponents;

public class OpenGraphMetadataViewComponent : ViewComponentBase
{
    private readonly IMetaDataTool _metaDataTool;
    private readonly ISoundWordsConfiguration _soundWordsConfiguration;

    public OpenGraphMetadataViewComponent(IMetaDataTool metaDataTool, ISoundWordsConfiguration soundWordsConfiguration)
    {
        _metaDataTool = metaDataTool;
        _soundWordsConfiguration = soundWordsConfiguration;
    }

    public IViewComponentResult Invoke(OpenGraphMetadata? data)
    {
        data ??= new OpenGraphMetadata();
        Metadata metaData = _metaDataTool.GetMetaData(new OpenGraphMetadata
                                                     {
                                                         Title = data.Title,
                                                         Description = data.Description ?? _soundWordsConfiguration.MetaDescription,
                                                         Url = data.Url ?? HttpContext.Request.GetEncodedUrl(),
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
