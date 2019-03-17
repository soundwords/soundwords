using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using JetBrains.Annotations;
using MoreLinq;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using SoundWords.Models;
using SoundWords.Tools;
using TagLib;
using Properties = TagLib.Properties;

namespace SoundWords.Services
{
    [Route("/feed/{Speaker*}")]
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class SpeakerFeedRequest
    {
        public string Speaker { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class FeedService : ServiceBase
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly Func<string, bool, File.IFileAbstraction> _fileAbstractionFactory;
        private readonly IFileSystem _fileSystem;

        public FeedService(IDbConnectionFactory dbConnectionFactory,
                           IFileSystem fileSystem, Func<string, bool, File.IFileAbstraction> fileAbstractionFactory)
        {
            _dbConnectionFactory = dbConnectionFactory;
            _fileSystem = fileSystem;
            _fileAbstractionFactory = fileAbstractionFactory;
        }

        public object Any(SpeakerFeedRequest speakerFeedRequest)
        {
            bool includeRestricted = UserSession.IsAuthenticated;
            const int limit = 50;
            XNamespace itunes = "http://www.itunes.com/dtds/podcast-1.0.dtd";
            XNamespace atom = "http://www.w3.org/2005/Atom";
            string siteUrl = Request.GetApplicationUrl();

            string logoUrl = $"{siteUrl}/content/images/podcast_logo.png";
            const string subscribe = @"<h4>Abonner i iTunes</h4>
<ol>
	<li>Start iTunes</li>
	<li>Klikk Fil - Abonner på podcast (File - Subscribe to Podcast). (Trykk Alt-F for å få frem menyen i Windows.)
	<li>Lim inn lenken til denne siden, og klikk OK</li>
</ol>";

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                DbSpeaker speaker = null;
                if (speakerFeedRequest.Speaker != null)
                {
                    NameInfo nameInfo = speakerFeedRequest.Speaker?.ToNameInfo();
                    speaker = db.Single<DbSpeaker>(s => s.FirstName == nameInfo.FirstName && s.LastName == nameInfo.LastName && !s.Deleted);
                }

                string description = string.Empty;

                SqlExpression<DbSpeaker> query = db.From<DbSpeaker>()
                                                   .Join<DbRecordingSpeaker>((sp, recordingSpeaker) => sp.Id == recordingSpeaker.SpeakerId)
                                                   .Join<DbRecordingSpeaker, DbRecording>((recordingSpeaker, recording) =>
                                                                                              recordingSpeaker.RecordingId == recording.Id)
                                                   .Join<DbRecording, DbAlbum>((recording, album) => recording.AlbumId == album.Id)
                                                   .Where<DbRecording, DbSpeaker>((recording, sp) => !recording.Deleted && !sp.Deleted)
                                                   .Take(limit);
                if (speaker != null)
                {
                    query.And<DbSpeaker>(s => s.Id == speaker.Id);
                }
                else
                {
                    query.OrderByDescending<DbRecording>(r => r.CreatedOn);
                }

                if (!includeRestricted)
                {
                    query.And<DbRecording>(r => r.Restricted == false);
                }

                List<Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> speakerInfo =
                    db.SelectMulti<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>(query);

                ILookup<long, Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> recordingLookup = speakerInfo.ToLookup(s => s.Item3.Id);

                Dictionary<long, DbAlbum> albums = speakerInfo.DistinctBy(r => r.Item4.Id).Select(r => r.Item4).ToDictionary(a => a.Id);

                IEnumerable<XElement> items =
                    from recording in speakerInfo.DistinctBy(r => r.Item3.Id).Select(r => r.Item3.ToRecording())
                    let album = albums[recording.AlbumId]
                    let speakerName = recordingLookup[recording.Id]
                        .Select(s => s.Item1).DistinctBy(s => s.Id)
                        .Select(s => s.ToSpeaker().FullName)
                        .ToSeparatedString('/')
                    let trackDescription =
                        $"{recording.Comment}{(recording.Year != null ? " ({0})".Fmt(recording.Year) : string.Empty)}".PadRight(1, '-')
                    let tagInfo = GetTag(recording)
                    let fileInfo = _fileSystem.FileInfo.FromFileName(recording.Path)
                    let titleSuffix =
                        tagInfo.Tag.TrackCount > 1
                            ? " ({0}/{1})".Fmt(tagInfo.Tag.Track, tagInfo.Tag.TrackCount)
                            : string.Empty
                    let url =
                        $"{siteUrl}/Recording/Stream/{recording.Uid:N}/{fileInfo.Name.UrlEncode20()}"
                    let guid =
                        $"{siteUrl}/Recording/Stream/{recording.Uid:N}{(speakerFeedRequest.Speaker == null ? "/top50" : string.Empty)}"
                    select new XElement("item",
                                        new XElement("title",
                                                     $"{album.Name}: {recording.Title}{titleSuffix}"
                                        ),
                                        new XElement(itunes + "author", speakerName
                                        ),
                                        new XElement(itunes + "subtitle",
                                                     $"{(speakerFeedRequest.Speaker == null ? $"{speakerName}: " : string.Empty)}{album.Name}"
                                        ),
                                        new XElement(itunes + "summary", new XCData(trackDescription)
                                        ),
                                        new XElement("description",
                                                     $"{trackDescription}{Environment.NewLine}{subscribe}"
                                        ),
                                        new XElement(itunes + "image",
                                                     new XAttribute("href",
                                                                    logoUrl)
                                        ),
                                        new XElement("enclosure",
                                                     new XAttribute("url", url),
                                                     new XAttribute("length", fileInfo.Length),
                                                     new XAttribute("type", "audio/mpeg")
                                        ),
                                        new XElement("guid", guid
                                        ),
                                        new XElement("pubDate", recording.CreatedOn.ToString("r")
                                        ),
                                        new XElement(itunes + "duration", tagInfo.Properties.Duration.ToString(@"hh\:mm\:ss")
                                        ),
                                        new XElement(itunes + "explicit", "no"
                                        )
                    );

                string title = speakerFeedRequest.Speaker ?? $"Siste {limit}";

                string link = speakerFeedRequest.Speaker != null
                                  ? $"{siteUrl}/Recording/Speaker/{speakerFeedRequest.Speaker.UrlEncode20()}"
                                  : siteUrl;

                string selfUrl = $"{siteUrl}{Request.RawUrl}";

                List<XElement> categories = new List<XElement>();
                for (int i = 0; i < Configuration.PodcastCategories.Count; i++)
                {
                    categories.Add(new XElement(itunes + "category",
                                                new XAttribute("text", Configuration.PodcastCategories[i]),
                                                new XElement(itunes + "category",
                                                             new XAttribute("text", Configuration.PodcastSubcategories[i])
                                                )
                                   ));
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
                                                           new XAttribute(XNamespace.Xmlns + "atom", atom)
                                              ),
                                              new XElement("title", $"{Configuration.SiteName}: {title}"
                                              ),
                                              new XElement("link", link
                                              ),
                                              new XElement("language", "no"
                                              ),
                                              new XElement("copyright", Configuration.CompanyName
                                              ),
                                              new XElement(itunes + "subtitle", Configuration.Slogan,
                                                           new XAttribute(XNamespace.Xmlns + "itunes", itunes)
                                              ),
                                              new XElement(itunes + "author", speakerFeedRequest.Speaker ?? Configuration.CompanyName
                                              ),
                                              new XElement(itunes + "summary",
                                                           new XCData(WebUtility.HtmlDecode(Configuration.MetaDescription))
                                              ),
                                              new XElement("description", description
                                              ),
                                              new XElement(itunes + "owner",
                                                           new XElement(itunes + "name", Configuration.CompanyName
                                                           ),
                                                           new XElement(itunes + "email", Configuration.CompanyEmail
                                                           )
                                              ),
                                              new XElement(itunes + "image",
                                                           new XAttribute("href", logoUrl)
                                              ),
                                              categories,
                                              new XElement(itunes + "explicit", "no"
                                              ),
                                              items
                                 )
                    );

                string podcastFeed = $@"<?xml version=""1.0"" encoding=""UTF-8""?>{Environment.NewLine}{element}";
                return new HttpResult(podcastFeed, "application/rss+xml");
            }
        }

        private TagInfo GetTag(Models.Recording recording)
        {
            using (File file = File.Create(_fileAbstractionFactory(recording.Path, false)))
            {
                return new TagInfo
                       {
                           Tag = file.Tag,
                           Properties = file.Properties
                       };
            }
        }

        private class TagInfo
        {
            public Tag Tag { get; set; }
            public Properties Properties { get; set; }
        }
    }
}
