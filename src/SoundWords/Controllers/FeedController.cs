using System.IO.Abstractions;
using System.Net;
using System.Xml.Linq;
using LinqToDB;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using SoundWords.Data;
using SoundWords.Media;
using SoundWords.Models;
using SoundWords.Tools;
using TagLib;
using TagFile = TagLib.File;

namespace SoundWords.Controllers;

public class FeedController : SoundWordsController
{
    private readonly Func<SoundWordsDb> _dbFactory;
    private readonly Func<string, bool, TagFile.IFileAbstraction> _fileAbstractionFactory;
    private readonly IFileSystem _fileSystem;
    private readonly ISoundWordsConfiguration _configuration;
    private readonly ISignedMediaUrls _mediaUrls;

    public FeedController(Func<SoundWordsDb> dbFactory,
                          IFileSystem fileSystem,
                          Func<string, bool, TagFile.IFileAbstraction> fileAbstractionFactory,
                          ISoundWordsConfiguration configuration,
                          ISignedMediaUrls mediaUrls)
    {
        _dbFactory = dbFactory;
        _fileSystem = fileSystem;
        _fileAbstractionFactory = fileAbstractionFactory;
        _configuration = configuration;
        _mediaUrls = mediaUrls;
    }

    [HttpGet("/feed/{**speaker}")]
    [HttpHead("/feed/{**speaker}")]
    public IActionResult Index(string? speaker)
    {
        // The RSS feed is consumed anonymously by aggregators (Apple Podcasts, Overcast,
        // …) hours or days after generation, long after any cookie expires. So we ALWAYS
        // exclude restricted recordings — enclosure URLs need to outlive the session, and
        // the public bucket exposes only non-restricted files.
        const int limit = 50;
        XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        XNamespace atom = "http://www.w3.org/2005/Atom";
        string? siteUrl = _configuration.SiteUrl;

        string logoUrl = $"{siteUrl}/content/images/podcast_logo.png";
        const string subscribe = @"<h4>Abonner i iTunes</h4>
<ol>
	<li>Start iTunes</li>
	<li>Klikk Fil - Abonner på podcast (File - Subscribe to Podcast). (Trykk Alt-F for å få frem menyen i Windows.)
	<li>Lim inn lenken til denne siden, og klikk OK</li>
</ol>";

        using SoundWordsDb db = _dbFactory();

        DbSpeaker? dbSpeaker = null;
        if (speaker != null)
        {
            NameInfo nameInfo = speaker.ToNameInfo();
            dbSpeaker = db.Speakers.SingleOrDefault(s => s.FirstName == nameInfo.FirstName
                                                                          && s.LastName == nameInfo.LastName
                                                                          && !s.Deleted);
        }

        var query = from sp in db.Speakers
                    join recordingSpeaker in db.RecordingSpeakers on sp.Id equals recordingSpeaker.SpeakerId
                    join recording in db.Recordings on recordingSpeaker.RecordingId equals recording.Id
                    join album in db.Albums on recording.AlbumId equals album.Id
                    where !recording.Deleted && !sp.Deleted && !recording.Restricted
                    select new { Speaker = sp, Recording = recording, Album = album };

        if (dbSpeaker != null)
        {
            DbSpeaker speakerRef = dbSpeaker;
            query = query.Where(x => x.Speaker.Id == speakerRef.Id);
        }
        else
        {
            query = query.OrderByDescending(x => x.Recording.CreatedOn);
        }

        var rows = query.Take(limit).ToList();

        ILookup<long, (DbSpeaker Speaker, DbRecording Recording, DbAlbum Album)> recordingLookup =
            rows.ToLookup(r => r.Recording.Id, r => (r.Speaker, r.Recording, r.Album));
        Dictionary<long, DbAlbum> albums = rows.DistinctBy(r => r.Album.Id).Select(r => r.Album).ToDictionary(a => a.Id);

        IEnumerable<XElement> items =
            from row in rows.DistinctBy(r => r.Recording.Id).Select(r => r.Recording.ToRecording())
            let album = albums[row.AlbumId]
            let speakerNames = string.Join('/', recordingLookup[row.Id]
                                                .Select(s => s.Speaker).DistinctBy(s => s.Id)
                                                .Select(s => s.ToSpeaker().FullName))
            let trackDescription =
                $"{row.Comment}{(row.Year != null ? $" ({row.Year})" : string.Empty)}".PadRight(1, '-')
            let tagInfo = GetTag(row)
            let fileInfo = _fileSystem.FileInfo.New(row.Path!)
            let titleSuffix =
                tagInfo.Tag.TrackCount > 1 ? $" ({tagInfo.Tag.Track}/{tagInfo.Tag.TrackCount})" : string.Empty
            let url = _mediaUrls.ForRecording(row)
            let guid = $"{siteUrl}/Recording/Stream/{row.Uid:N}{(speaker == null ? "/top50" : string.Empty)}"
            select new XElement("item",
                                new XElement("title", $"{album.Name}: {row.Title}{titleSuffix}"),
                                new XElement(itunes + "author", speakerNames),
                                new XElement(itunes + "subtitle",
                                             $"{(speaker == null ? $"{speakerNames}: " : string.Empty)}{album.Name}"),
                                new XElement(itunes + "summary", new XCData(trackDescription)),
                                new XElement("description", $"{trackDescription}{Environment.NewLine}{subscribe}"),
                                new XElement(itunes + "image", new XAttribute("href", logoUrl)),
                                new XElement("enclosure",
                                             new XAttribute("url", url),
                                             new XAttribute("length", fileInfo.Length),
                                             new XAttribute("type", "audio/mpeg")),
                                new XElement("guid", guid),
                                new XElement("pubDate", row.CreatedOn.ToString("r")),
                                new XElement(itunes + "duration",
                                             tagInfo.Properties.Duration.ToString(@"hh\:mm\:ss")),
                                new XElement(itunes + "explicit", "no"));

        string title = speaker ?? $"Siste {limit}";
        string link = speaker != null
                          ? $"{siteUrl}/Recording/Speaker/{speaker.UrlEncode20()}"
                          : siteUrl ?? "/";
        string selfUrl = $"{siteUrl}{HttpContext.Request.GetEncodedPathAndQuery()}";

        List<XElement> categories = new();
        for (int i = 0; i < _configuration.PodcastCategories.Count; i++)
        {
            categories.Add(new XElement(itunes + "category",
                                        new XAttribute("text", _configuration.PodcastCategories[i]),
                                        new XElement(itunes + "category",
                                                     new XAttribute("text", _configuration.PodcastSubcategories[i]))));
        }

        XElement element =
            new XElement("rss",
                         new XAttribute(XNamespace.Xmlns + "itunes", "http://www.itunes.com/dtds/podcast-1.0.dtd"),
                         new XAttribute(XNamespace.Xmlns + "atom", "http://www.w3.org/2005/Atom"),
                         new XAttribute("version", "2.0"),
                         new XElement("channel",
                                      new XElement(atom + "link",
                                                   new XAttribute("href", selfUrl),
                                                   new XAttribute("rel", "self"),
                                                   new XAttribute("type", "application/rss+xml"),
                                                   new XAttribute(XNamespace.Xmlns + "atom", atom)),
                                      new XElement("title", $"{_configuration.SiteName}: {title}"),
                                      new XElement("link", link),
                                      new XElement("language", "no"),
                                      new XElement("copyright", _configuration.CompanyName ?? string.Empty),
                                      new XElement(itunes + "subtitle", _configuration.Slogan ?? string.Empty,
                                                   new XAttribute(XNamespace.Xmlns + "itunes", itunes)),
                                      new XElement(itunes + "author", speaker ?? _configuration.CompanyName ?? string.Empty),
                                      new XElement(itunes + "summary",
                                                   new XCData(WebUtility.HtmlDecode(_configuration.MetaDescription ?? string.Empty))),
                                      new XElement("description", string.Empty),
                                      new XElement(itunes + "owner",
                                                   new XElement(itunes + "name", _configuration.CompanyName ?? string.Empty),
                                                   new XElement(itunes + "email", _configuration.CompanyEmail ?? string.Empty)),
                                      new XElement(itunes + "image", new XAttribute("href", logoUrl)),
                                      categories,
                                      new XElement(itunes + "explicit", "no"),
                                      items));

        string podcastFeed = $@"<?xml version=""1.0"" encoding=""UTF-8""?>{Environment.NewLine}{element}";
        return Content(podcastFeed, "application/rss+xml");
    }

    private TagInfo GetTag(Recording recording)
    {
        using TagFile file = TagFile.Create(_fileAbstractionFactory(recording.Path!, false));
        return new TagInfo
               {
                   Tag = file.Tag,
                   Properties = file.Properties
               };
    }

    private class TagInfo
    {
        public Tag Tag { get; set; } = null!;
        public TagLib.Properties Properties { get; set; } = null!;
    }
}
