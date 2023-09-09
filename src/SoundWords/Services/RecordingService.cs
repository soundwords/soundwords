using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using SoundWords.Models;
using SoundWords.Social;
using SoundWords.Tools;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using static MoreLinq.Extensions.ToDelimitedStringExtension;

namespace SoundWords.Services
{
    [Route("/Recording")]
    public class Recording : IReturn<RecordingResponse>
    {
    }

    public class RecordingResponse
    {
        public List<Models.Recording> Recordings { get; set; }
    }

    //[Route("/Recording/Details/{Id}")]
    //public class Details
    //{
    //    public int Id { get; set; }
    //}

    //public class DetailsResponse
    //{
    //    public Models.Recording Recording { get; set; }
    //}

    //[Route("/Recording/Delete/{Id}")]
    //public class Delete
    //{
    //    public int Id { get; set; }
    //}

    //public class DeleteResponse
    //{
    //    public Models.Recording Recording { get; set; }
    //}

    [Route("/Recording/Speaker/{Name}")]
    [EscapedFragment]
    public class SpeakerDetails : IReturn<SpeakerDetailsResponse>, IHaveEscapedFragment
    {
        public string Name { get; set; }
        public string Album { get; set; }
        public string EscapedFragment { get; set; }
    }

    public class SpeakerDetailsResponse
    {
        public string Uid { get; set; }
        public string Speaker { get; set; }
        public List<AlbumInfo> Albums { get; set; }
        public List<SpeakerInfo> Speakers { get; set; }
        public AlbumInfo SelectedAlbum { get; set; }
        public string Description { get; set; }
        public bool HasPhoto { get; set; }
    }

    //[Route("/Recording/SpeakersPartial")]
    //[Route("/Recording/SpeakersPartial/{Selected}")]
    //public class SpeakersPartial
    //{
    //    public string Selected { get; set; }
    //}

    //public class SpeakersPartialResponse
    //{
    //    public string Selected { get; set; }
    //    public List<string> Speakers { get; set; }
    //}

    [Route("/Recording/Stream/{Id*}")]
    public class Stream
    {
        public string Id { get; set; }
    }

    [Route("/Recording/Download/{Uid}")]
    public class Download
    {
        public string Uid { get; set; }
    }

    [Route("/Recording/Rebuild")]
    public class Rebuild : IReturnVoid
    {
        public string From { get; set; }
    }

    [Route("/Album/DownloadAttachment/{AlbumUid}/{Index}")]
    public class DownloadAttachment
    {
        public string AlbumUid { get; set; }
        public int Index { get; set; }
    }

    [Route("/Album/AlbumArt/{AlbumUid}")]
    public class AlbumArt
    {
        public string AlbumUid { get; set; }
        public int? MaxDimension { get; set; }
    }


    [Route("/Speaker/Photo/{SpeakerUid}")]
    public class SpeakerPhoto
    {
        public string SpeakerUid { get; set; }
        public int? MaxDimension { get; set; }
    }

    [Route("/Album/IsRestricted")]
    [Route("/Album/IsRestricted/{SpeakerName}")]
    public class IsRestricted : IReturn<IsRestrictedResponse>
    {
        public string SpeakerName { get; set; }
    }

    public class IsRestrictedResponse
    {
        public bool IsRestricted { get; set; }
    }

    public class RecordingService : ServiceBase
    {
        private readonly ILog _logger;
        private readonly IRecordingRepository _recordingRepository;
        private readonly ISoundWordsConfiguration _soundWordsConfiguration;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IServerEvents _serverEvents;
        private readonly IBackgroundPool _backgroundPool;

        public RecordingService(ILogFactory loggerFactory, IRecordingRepository recordingRepository, ISoundWordsConfiguration soundWordsConfiguration, IDbConnectionFactory dbConnectionFactory, IFileSystem fileSystem, IServerEvents serverEvents, IBackgroundPool backgroundPool)
        {
            _logger = loggerFactory.GetLogger(GetType());
            _recordingRepository = recordingRepository;
            _soundWordsConfiguration = soundWordsConfiguration;
            _dbConnectionFactory = dbConnectionFactory;
            _fileSystem = fileSystem;
            _serverEvents = serverEvents;
            _backgroundPool = backgroundPool;
        }

        public RecordingResponse Get(Recording recording)
        {
            var includeRestricted = UserSession.IsAuthenticated;
            var allRecordings = _recordingRepository.GetAllRecordings(includeRestricted).ToList();
            return new RecordingResponse { Recordings = allRecordings };
        }

        public IsRestrictedResponse Get(IsRestricted request)
        {
            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                NameInfo nameInfo = request.SpeakerName.ToNameInfo();

                bool restrictedAvailable = db.Exists(db.From<DbRecording>()
                                                       .Join<DbRecordingSpeaker>((recording, recordingSpeaker) => recording.Id == recordingSpeaker.RecordingId)
                                                       .Join<DbRecordingSpeaker, DbSpeaker>((recordingSpeaker, speaker) =>
                                                                                                recordingSpeaker.SpeakerId == speaker.Id)
                                                       .Where<DbRecording, DbSpeaker>((recording, speaker) =>
                                                                                          recording.Restricted && !speaker.Deleted && !recording.Deleted && speaker.FirstName == nameInfo.FirstName &&
                                                                                          speaker.LastName == nameInfo.LastName));

                return new IsRestrictedResponse
                       {
                           IsRestricted = restrictedAvailable
                       };
            }
        }

        //public object Get(Details details)
        //{
        //    Models.Recording recording = _recordingRepository.GetById(details.Id);
        //    if (recording.Restricted && !UserSession.IsAuthenticated)
        //        return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));

        //    return new DetailsResponse {Recording = recording};
        //}        

        //public object Get(Delete delete)
        //{
        //    Models.Recording recording = _recordingRepository.GetById(delete.Id);
        //    if (recording.Restricted && !UserSession.IsAuthenticated) return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));


        //    return new DeleteResponse {Recording = recording};
        //}

        //[Authenticate]
        //[RequiredRole(RoleNames.Admin)]
        //public object Post(Delete delete)
        //{
        //    _recordingRepository.Delete(delete.Id);

        //    return this.Redirect("/Recording");
        //}

        public object Get(SpeakerDetails speaker)
        {
            if (speaker.EscapedFragment == null && !Request.RawUrl.IsNormalizedUrl())
            {
                return this.RedirectPermanently(Request.RawUrl.ToNormalizedUrl());
            }

            if (speaker.EscapedFragment != null && speaker.EscapedFragment.StartsWith("/"))
            {
                speaker.Album = speaker.EscapedFragment.Substring(1);
            }

            var includeRestricted = UserSession.IsAuthenticated;

            NameInfo nameInfo = speaker.Name.ToNameInfo();
            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                DbSpeaker dbSpeaker = db.Single<DbSpeaker>(s => s.FirstName == nameInfo.FirstName && s.LastName == nameInfo.LastName && !s.Deleted);
                if (dbSpeaker == null)
                {
                    throw HttpError.NotFound("Speaker not found");
                }

                SqlExpression<DbRecording> albumIdQuery =
                    db.From<DbRecording>()
                      .Join<DbRecordingSpeaker>((recording, recordingSpeaker) => recording.Id == recordingSpeaker.RecordingId)
                      .Where<DbRecording, DbRecordingSpeaker>((recording, recordingSpeaker) => !recording.Deleted && recordingSpeaker.SpeakerId == dbSpeaker.Id)
                      .SelectDistinct(rs => rs.AlbumId);

                SqlExpression<DbSpeaker> query = db.From<DbSpeaker>()
                                                   .Join<DbRecordingSpeaker>((sp, recordingSpeaker) => sp.Id == recordingSpeaker.SpeakerId)
                                                   .Join<DbRecordingSpeaker, DbRecording>((recordingSpeaker, recording) => recordingSpeaker.RecordingId == recording.Id)
                                                   .Join<DbRecording, DbAlbum>((recording, album) => recording.AlbumId == album.Id)
                                                   .Where<DbSpeaker, DbAlbum>((sp, album) => !sp.Deleted && !album.Deleted && Sql.In(album.Id, albumIdQuery))
                                                   .OrderBy<DbAlbum>(a => a.Name)
                                                   .ThenBy<DbRecording>(r => r.Track);

                if (!includeRestricted)
                {
                    query.And<DbAlbum>(a => !a.Restricted);
                }

                List<Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> recordings = db.SelectMulti<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>(query);

                Dictionary<long, DbAlbum> albums = recordings.DistinctBy(r => r.Item4.Id).Select(r => r.Item4).ToDictionary(a => a.Id);

                ILookup<long, Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> albumLookup = recordings.ToLookup(r => r.Item4.Id);
                ILookup<long, Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> speakers = recordings.ToLookup(r => r.Item3.Id);

                List<AlbumInfo> albumInfos =
                    (from g in albumLookup
                     let album = albums[g.Key]
                     select new AlbumInfo
                            {
                                Uid = album.Uid,
                                Name = album.Name,
                                Description = album.Description,
                                AlbumSpeakers = GetSpeakers(g),
                                HasAlbumArt = album.AlbumArtPath != null,
                                Recordings = g.DistinctBy(r => r.Item3.Id)
                                              .OrderBy(r => r.Item3.Track)
                                              .Select(r => new RecordingInfo
                                                           {
                                                               Uid = r.Item3.Uid,
                                                               Title = r.Item3.Title,
                                                               Track = r.Item3.Track,
                                                               Year = r.Item3.Year,
                                                               Comment = r.Item3.Comment,
                                                               Speakers = speakers[r.Item3.Id]
                                                                   .DistinctBy(rs => rs.Item1.Id)
                                                                   .Select(rs => rs.Item1.ToSpeakerInfo())
                                                                   .ToList()
                                                           }).ToList(),
                                Attachments = album.AttachmentPaths
                                                   .Select((attachment, index) => new AttachmentInfo
                                                                                  {
                                                                                      Name =
                                                                                          _fileSystem.Path.GetFileName(attachment),
                                                                                      Index = index
                                                                                  }).ToList()
                            }).ToList();

                return new SpeakerDetailsResponse
                {
                    Uid = dbSpeaker.Uid,
                    Speaker = speaker.Name,
                    Albums = albumInfos.ToList(),
                    Speakers = _recordingRepository.GetSpeakers(includeRestricted).Select(s => s.ToSpeakerInfo(sp => sp.FullName == speaker.Name)).ToList(),
                    SelectedAlbum = albumInfos.FirstOrDefault(a => a.Name == (speaker.Album ?? speaker.EscapedFragment)),
                    HasPhoto = dbSpeaker.PhotoPath != null,
                    Description = dbSpeaker.Description
                };
            }
        }

        private string GetSpeakers(IGrouping<long, Tuple<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>> grouping)
        {
            List<string> speakers = grouping.Select(i => i.Item1)
                                                   .DistinctBy(i => i.Id).OrderBy(s => s.LastName)
                                                   .ThenBy(s => s.FirstName)
                                                   .Select(s => s.ToSpeaker().FullName)
                                                   .ToList();


            return speakers.Take(speakers.Count - 2).Concat(new [] { speakers.Skip(speakers.Count - 2).ToDelimitedString(" og ") })
                           .ToDelimitedString(", ");
        }

        //[RequiredRole(RoleNames.Admin)]
        public void Post(Rebuild rebuild)
        {
            SubscriptionInfo subscriptionInfo = _serverEvents.GetSubscriptionInfo(rebuild.From);
            if (subscriptionInfo == null) throw HttpError.NotFound("Subscription {0} does not exist".Fmt(rebuild.From));

            _backgroundPool.Enqueue<RebuildJob, SubscriptionInfo>(subscriptionInfo);
        }


        //public object Get(SpeakersPartial speakersPartial)
        //{
        //    return new SpeakersPartialResponse {Speakers = _recordingRepository.GetSpeakers(UserSession.IsAuthenticated), Selected = speakersPartial.Selected};
        //}

        public object Get(Stream stream)
        {
            if (stream.Id == null) throw new ArgumentNullException("id");
            string[] idParts = stream.Id.Split('/');

            Models.Recording recording = _recordingRepository.GetById(idParts[0]);
            var isAuthenticated = UserSession.IsAuthenticated;
            if (recording.Restricted && !isAuthenticated) return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));

            FileInfo fileInfo = new FileInfo(recording.Path);
            string contentType = GetMimeType(fileInfo.Name);
            return new HttpResult(fileInfo, contentType);
        }

        public object Get(Download download)
        {
            if (download.Uid == null) throw new ArgumentNullException("id");

            Models.Recording recording = _recordingRepository.GetById(download.Uid);
            var isAuthenticated = UserSession.IsAuthenticated;
            if (recording.Restricted && !isAuthenticated) return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));

            FileInfo fileInfo = new FileInfo(recording.Path);
            string contentType = GetMimeType(fileInfo.Name);
            return new HttpResult(fileInfo, contentType, true);
        }

        public object Get(DownloadAttachment downloadAttachment)
        {
            if (downloadAttachment.AlbumUid == null) throw new ArgumentNullException("albumUid", "The AlbumUid parameter must be a Guid");

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                                Album album = db.Single<DbAlbum>(a => a.Uid == downloadAttachment.AlbumUid).ToAlbum();
                string attachmentPath = album.AttachmentPaths[downloadAttachment.Index];

                var isAuthenticated = UserSession.IsAuthenticated;
                if (album.Restricted && !isAuthenticated) return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));

                FileInfo fileInfo = new FileInfo(attachmentPath);
                string contentType = GetMimeType(fileInfo.Name);
                return new HttpResult(fileInfo, contentType, true);
            }
        }

        public object Get(AlbumArt albumArt)
        {
            if (albumArt.AlbumUid == null) throw new ArgumentNullException("albumUid", "The AlbumUid parameter must be a Guid");

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                Album album = db.Single<DbAlbum>(a => a.Uid == albumArt.AlbumUid).ToAlbum();
                if (album.AlbumArtPath == null) return null;

                FileInfo fileResponse = new FileInfo(album.AlbumArtPath);
                if (albumArt.MaxDimension == null)
                {
                    return new HttpResult(fileResponse, GetMimeType(fileResponse.Name));
                }

                var cacheFileName =
                    $"{_soundWordsConfiguration.CachePath}/{album.Uid}#{albumArt.MaxDimension}{fileResponse.Extension}";

                if (!_fileSystem.File.Exists(cacheFileName))
                {
                    if (!_fileSystem.Directory.Exists(_soundWordsConfiguration.CachePath)) _fileSystem.Directory.CreateDirectory(_soundWordsConfiguration.CachePath);

                    CreateCacheFile(album.AlbumArtPath, cacheFileName, (int)albumArt.MaxDimension);
                }
                else
                {
                    _logger.DebugFormat("Found image in cache: {0}", cacheFileName);
                }

                FileInfo fileInfo = new FileInfo(cacheFileName);
                string contentType = GetMimeType(fileInfo.Name);
                return new HttpResult(fileInfo, contentType);
            }
        }

        public object Get(SpeakerPhoto speakerPhoto)
        {
            if (speakerPhoto.SpeakerUid == null) throw new ArgumentNullException("speakerPhoto", "The SpeakerUid parameter must be a Guid");

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                Models.Speaker speaker = db.Single<DbSpeaker>(s => !s.Deleted && s.Uid == speakerPhoto.SpeakerUid).ToSpeaker();
                if (speaker == null) throw HttpError.NotFound("Couldn't find a photo with the given ID.");

                FileInfo fileResponse = new FileInfo(speaker.PhotoPath);
                if (speakerPhoto.MaxDimension == null)
                {
                    return new HttpResult(fileResponse, GetMimeType(fileResponse.Name));
                }

                var cacheFileName =
                    $"{_soundWordsConfiguration.CachePath}/{speaker.Uid}#{speakerPhoto.MaxDimension}{fileResponse.Extension}";

                if (!_fileSystem.File.Exists(cacheFileName))
                {
                    if (!_fileSystem.Directory.Exists(_soundWordsConfiguration.CachePath)) _fileSystem.Directory.CreateDirectory(_soundWordsConfiguration.CachePath);

                    CreateCacheFile(speaker.PhotoPath, cacheFileName, (int)speakerPhoto.MaxDimension);
                }
                else
                {
                    _logger.DebugFormat("Found image in cache: {0}", cacheFileName);
                }

                FileInfo fileInfo = new FileInfo(cacheFileName);
                string contentType = GetMimeType(fileInfo.Name);
                return new HttpResult(fileInfo, contentType);
            }
        }

        private void CreateCacheFile(string albumArtPath, string cacheFileName, int maxDimension)
        {
            _logger.DebugFormat("Resizing immage {0} to size {1}", albumArtPath, maxDimension);

            const int quality = 70;
            CreateCacheFileImageFactory(albumArtPath, cacheFileName, maxDimension, quality);
        }

        private static void CreateCacheFileImageFactory(string albumArtPath, string cacheFileName, int maxDimension, int quality)
        {
            JpegEncoder jpegEncoder = new JpegEncoder { Quality = quality }; 
          
            Size size = new Size(maxDimension, 0);
            using (var image = Image.Load(albumArtPath))
            {
                image.Mutate(x => x.Resize(size));
                image.Save(cacheFileName, jpegEncoder);
            }
        }


        ////
        //// GET: /Recording/Stream/5

        //public object Thumbnail(int id)
        //{
        //    if (id <= 0) throw new ArgumentOutOfRangeException("id", "The id parameter must be a positive, non-zero number");

        //    Recording recording = _recordingRepository.GetById(id);
        //    if (recording.Restricted && !UserSession.IsAuthenticated) return HttpResult.Redirect("/Login".AddQueryParam("redirect", Request.AbsoluteUri));

        //    IPicture picture = TagLib.File.Create(recording.Path).Tag.Pictures.FirstOrDefault();
        //    if (picture == null) return null;
        //    return File(picture.Data.ToArray(), picture.MimeType);
        //}

        private static string GetMimeType(string fileName)
        {
            string mimeType = MimeTypes.GetMimeType(fileName);
            if (mimeType == "audio/mpeg3") return "audio/mpeg";
            return mimeType;
        }
    }

    public class SpeakerInfo
    {
        public string Uid { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public bool Selected { get; set; }
        public bool HasPhoto { get; set; }
        public string Description { get; set; }
    }

    public class AlbumInfo
    {
        public string Uid { get; set; }
        public string AlbumSpeakers { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool HasAlbumArt { get; set; }
        public List<RecordingInfo> Recordings { get; set; }
        public List<AttachmentInfo> Attachments { get; set; }
    }

    public class RecordingInfo
    {
        public string Uid { get; set; }
        public string Title { get; set; }
        public int Track { get; set; }
        public int? Year { get; set; }
        public string Comment { get; set; }
        public List<SpeakerInfo> Speakers { get; set; }
    }

    public class AttachmentInfo
    {
        public int Index { get; set; }
        public string Name { get; set; }
    }
}