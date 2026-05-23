using System.IO.Abstractions;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using SoundWords.Data;
using SoundWords.Filters;
using SoundWords.Models;
using SoundWords.Tools;

namespace SoundWords.Controllers;

public class RecordingController : SoundWordsController
{
    private readonly Func<SoundWordsDb> _dbFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IBackgroundPool _backgroundPool;
    private readonly ILogger<RecordingController> _logger;
    private readonly IRecordingRepository _recordingRepository;
    private readonly ISoundWordsConfiguration _soundWordsConfiguration;

    public RecordingController(ILogger<RecordingController> logger,
                               IRecordingRepository recordingRepository,
                               ISoundWordsConfiguration soundWordsConfiguration,
                               Func<SoundWordsDb> dbFactory,
                               IFileSystem fileSystem,
                               IBackgroundPool backgroundPool)
    {
        _logger = logger;
        _recordingRepository = recordingRepository;
        _soundWordsConfiguration = soundWordsConfiguration;
        _dbFactory = dbFactory;
        _fileSystem = fileSystem;
        _backgroundPool = backgroundPool;
    }

    [HttpGet("/Recording")]
    public IActionResult Index()
    {
        return View(new RecordingResponse
                    {
                        Recordings = _recordingRepository.GetAllRecordings(IncludeRestricted)
                    });
    }

    [HttpGet("/Album/IsRestricted")]
    [HttpGet("/Album/IsRestricted/{speakerName}")]
    public IActionResult IsRestricted(string? speakerName)
    {
        using SoundWordsDb db = _dbFactory();
        NameInfo? nameInfo = speakerName?.ToNameInfo();
        bool restrictedAvailable = false;
        if (nameInfo != null)
        {
            restrictedAvailable = (from recording in db.Recordings
                                   join recordingSpeaker in db.RecordingSpeakers
                                       on recording.Id equals recordingSpeaker.RecordingId
                                   join speaker in db.Speakers on recordingSpeaker.SpeakerId equals speaker.Id
                                   where recording.Restricted
                                                            && !recording.Deleted
                                                            && !speaker.Deleted
                                                            && speaker.FirstName == nameInfo.FirstName
                                                            && speaker.LastName == nameInfo.LastName
                                   select recording).Any();
        }

        return Json(new IsRestrictedResponse { IsRestricted = restrictedAvailable });
    }

    [HttpGet("/Recording/Speaker/{name}")]
    [EscapedFragment]
    public IActionResult SpeakerDetails(SpeakerDetailsRequest speaker)
    {
        string pathAndQuery = HttpContext.Request.GetEncodedPathAndQuery();
        if (speaker.EscapedFragment == null && !pathAndQuery.HasTrailingSlash())
        {
            return RedirectPermanent(pathAndQuery.WithTrailingSlash());
        }

        if (speaker.EscapedFragment != null && speaker.EscapedFragment.StartsWith('/'))
        {
            speaker.Album = speaker.EscapedFragment[1..];
        }

        NameInfo nameInfo = speaker.Name.ToNameInfo();
        using SoundWordsDb db = _dbFactory();

        DbSpeaker? dbSpeaker = db.Speakers.SingleOrDefault(s =>
                                                              s.FirstName == nameInfo.FirstName
                                                                                && s.LastName == nameInfo.LastName
                                                                                && !s.Deleted);
        if (dbSpeaker == null)
        {
            return NotFound("Speaker not found");
        }

        var query = from sp in db.Speakers
                    join recordingSpeaker in db.RecordingSpeakers on sp.Id equals recordingSpeaker.SpeakerId
                    join recording in db.Recordings on recordingSpeaker.RecordingId equals recording.Id
                    join alb in db.Albums on recording.AlbumId equals alb.Id
                    where !sp.Deleted && !alb.Deleted && !recording.Deleted
                                                     && db.Recordings
                                                          .Where(r => !r.Deleted)
                                                          .Join(db.RecordingSpeakers,
                                                                r => r.Id,
                                                                rs => rs.RecordingId,
                                                                (r, rs) => new { r, rs })
                                                          .Where(x => x.rs.SpeakerId == dbSpeaker.Id)
                                                          .Select(x => x.r.AlbumId)
                                                          .Distinct()
                                                          .Contains(alb.Id)
                    orderby alb.Name, recording.Track
                    select new
                           {
                               Speaker = sp,
                               Recording = recording,
                               Album = alb
                           };

        if (!IncludeRestricted)
        {
            query = query.Where(x => !x.Album.Restricted);
        }

        var rows = query.ToList();

        Dictionary<long, DbAlbum> albums = rows.DistinctBy(r => r.Album.Id).Select(r => r.Album).ToDictionary(a => a.Id);
        ILookup<long, (DbSpeaker Speaker, DbRecording Recording, DbAlbum Album)> albumLookup =
            rows.ToLookup(r => r.Album.Id, r => (r.Speaker, r.Recording, r.Album));
        ILookup<long, (DbSpeaker Speaker, DbRecording Recording, DbAlbum Album)> recordingLookup =
            rows.ToLookup(r => r.Recording.Id, r => (r.Speaker, r.Recording, r.Album));

        List<AlbumInfo> albumInfos =
            (from g in albumLookup
             let album = albums[g.Key]
             select new AlbumInfo
                    {
                        Uid = album.Uid,
                        Name = album.Name,
                        Description = album.Description,
                        AlbumSpeakers = JoinSpeakers(g),
                        HasAlbumArt = album.AlbumArtPath != null,
                        Recordings = g.DistinctBy(r => r.Recording.Id)
                                      .OrderBy(r => r.Recording.Track)
                                      .Select(r => new RecordingInfo
                                                   {
                                                       Uid = r.Recording.Uid,
                                                       Title = r.Recording.Title,
                                                       Track = r.Recording.Track,
                                                       Year = r.Recording.Year,
                                                       Comment = r.Recording.Comment,
                                                       Speakers = recordingLookup[r.Recording.Id]
                                                                  .DistinctBy(rs => rs.Speaker.Id)
                                                                  .Select(rs => rs.Speaker.ToSpeakerInfo())
                                                                  .ToList()
                                                   }).ToList(),
                        Attachments = (album.AttachmentPaths ?? new List<string>())
                                      .Select((attachment, index) => new AttachmentInfo
                                                                     {
                                                                         Name = _fileSystem.Path.GetFileName(attachment),
                                                                         Index = index
                                                                     }).ToList()
                    }).ToList();

        return View("SpeakerDetails", new SpeakerDetailsResponse
                                      {
                                          Uid = dbSpeaker.Uid,
                                          Speaker = speaker.Name,
                                          Albums = albumInfos.ToList(),
                                          Speakers = _recordingRepository.GetSpeakers(IncludeRestricted)
                                                                         .Select(s => s.ToSpeakerInfo(sp => sp.FullName == speaker.Name))
                                                                         .ToList(),
                                          SelectedAlbum = albumInfos.FirstOrDefault(a => a.Name == (speaker.Album ?? speaker.EscapedFragment)),
                                          HasPhoto = dbSpeaker.PhotoPath != null,
                                          Description = dbSpeaker.Description
                                      });
    }

    private static string JoinSpeakers(IEnumerable<(DbSpeaker Speaker, DbRecording Recording, DbAlbum Album)> grouping)
    {
        List<string> speakers = grouping.Select(i => i.Speaker)
                                        .DistinctBy(i => i.Id)
                                        .OrderBy(s => s.LastName)
                                        .ThenBy(s => s.FirstName)
                                        .Select(s => s.ToSpeaker().FullName)
                                        .ToList();

        if (speakers.Count < 2)
        {
            return string.Join(", ", speakers);
        }

        return string.Join(", ",
                           speakers.Take(speakers.Count - 2)
                                   .Concat(new[] { string.Join(" og ", speakers.Skip(speakers.Count - 2)) }));
    }

    [HttpGet("/Recording/Stream/{**id}")]
    public IActionResult Stream(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        string[] idParts = id.Split('/');
        Recording? recording = _recordingRepository.GetById(idParts[0]);
        if (recording == null)
        {
            return NotFound();
        }
        if (recording.Restricted && !IncludeRestricted)
        {
            return Redirect("/Login".AddQueryParam("redirect", HttpContext.Request.GetEncodedUrl()));
        }

        FileInfo fileInfo = new(recording.Path!);
        return PhysicalFile(fileInfo.FullName, GetMimeType(fileInfo.Name), enableRangeProcessing: true);
    }

    [HttpGet("/Recording/Download/{uid}")]
    public IActionResult Download(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            throw new ArgumentNullException(nameof(uid));
        }

        Recording? recording = _recordingRepository.GetById(uid);
        if (recording == null)
        {
            return NotFound();
        }
        if (recording.Restricted && !IncludeRestricted)
        {
            return Redirect("/Login".AddQueryParam("redirect", HttpContext.Request.GetEncodedUrl()));
        }

        FileInfo fileInfo = new(recording.Path!);
        return PhysicalFile(fileInfo.FullName, GetMimeType(fileInfo.Name), fileInfo.Name);
    }

    [HttpPost("/Recording/Rebuild")]
    [Authorize]
    public IActionResult Rebuild([FromQuery] string? jobId)
    {
        if (string.IsNullOrEmpty(jobId))
        {
            jobId = Guid.NewGuid().ToString("N");
        }

        _backgroundPool.Enqueue<RebuildJob, string>(jobId);
        return Accepted(new { jobId });
    }

    private static string GetMimeType(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".flac" => "audio/flac",
            ".ogg" => "audio/ogg",
            ".wav" => "audio/wav",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    [HttpGet("/Album/DownloadAttachment/{albumUid}/{index:int}")]
    public IActionResult DownloadAttachment(string albumUid, int index)
    {
        if (string.IsNullOrEmpty(albumUid))
        {
            throw new ArgumentNullException(nameof(albumUid), "The AlbumUid parameter must be a Guid");
        }

        using SoundWordsDb db = _dbFactory();
        DbAlbum? dbAlbum = db.Albums.SingleOrDefault(a => a.Uid == albumUid);
        if (dbAlbum == null || dbAlbum.AttachmentPaths == null || index >= dbAlbum.AttachmentPaths.Count)
        {
            return NotFound();
        }

        string attachmentPath = dbAlbum.AttachmentPaths[index];
        if (dbAlbum.Restricted && !IncludeRestricted)
        {
            return Redirect("/Login".AddQueryParam("redirect", HttpContext.Request.GetEncodedUrl()));
        }

        FileInfo fileInfo = new(attachmentPath);
        return PhysicalFile(fileInfo.FullName, GetMimeType(fileInfo.Name), fileInfo.Name);
    }

    [HttpGet("/Album/AlbumArt/{albumUid}")]
    public IActionResult AlbumArt(string albumUid, [FromQuery] int? maxDimension)
    {
        if (string.IsNullOrEmpty(albumUid))
        {
            throw new ArgumentNullException(nameof(albumUid), "The AlbumUid parameter must be a Guid");
        }

        using SoundWordsDb db = _dbFactory();
        DbAlbum? dbAlbum = db.Albums.SingleOrDefault(a => a.Uid == albumUid);
        if (dbAlbum?.AlbumArtPath == null)
        {
            return NotFound();
        }

        return ServeImage(dbAlbum.AlbumArtPath, dbAlbum.Uid, maxDimension);
    }

    [HttpGet("/Speaker/Photo/{speakerUid}")]
    public IActionResult SpeakerPhoto(string speakerUid, [FromQuery] int? maxDimension)
    {
        if (string.IsNullOrEmpty(speakerUid))
        {
            throw new ArgumentNullException(nameof(speakerUid), "The SpeakerUid parameter must be a Guid");
        }

        using SoundWordsDb db = _dbFactory();
        DbSpeaker? dbSpeaker = db.Speakers.SingleOrDefault(s => !s.Deleted && s.Uid == speakerUid);
        if (dbSpeaker?.PhotoPath == null)
        {
            return NotFound("Couldn't find a photo with the given ID.");
        }

        return ServeImage(dbSpeaker.PhotoPath, dbSpeaker.Uid, maxDimension);
    }

    private IActionResult ServeImage(string sourcePath, string uid, int? maxDimension)
    {
        FileInfo source = new(sourcePath);
        if (maxDimension == null)
        {
            return PhysicalFile(source.FullName, GetMimeType(source.Name));
        }

        string cacheFileName = $"{_soundWordsConfiguration.CachePath}/{uid}#{maxDimension}{source.Extension}";
        if (!_fileSystem.File.Exists(cacheFileName))
        {
            if (!_fileSystem.Directory.Exists(_soundWordsConfiguration.CachePath))
            {
                _fileSystem.Directory.CreateDirectory(_soundWordsConfiguration.CachePath);
            }

            CreateCacheFile(sourcePath, cacheFileName, maxDimension.Value);
        }
        else
        {
            _logger.LogDebug("Found image in cache: {Path}", cacheFileName);
        }

        FileInfo cached = new(cacheFileName);
        return PhysicalFile(cached.FullName, GetMimeType(cached.Name));
    }

    private void CreateCacheFile(string sourcePath, string cacheFileName, int maxDimension)
    {
        _logger.LogDebug("Resizing image {Path} to size {Size}", sourcePath, maxDimension);
        const int quality = 70;

        using SKBitmap source = SKBitmap.Decode(sourcePath)
                                ?? throw new InvalidOperationException($"Failed to decode image: {sourcePath}");

        // Match the old ImageSharp behaviour: width = maxDimension, height auto-scaled to preserve aspect ratio.
        int targetWidth = Math.Min(maxDimension, source.Width);
        int targetHeight = (int)Math.Round(source.Height * ((double)targetWidth / source.Width));

        using SKBitmap resized = source.Resize(new SKImageInfo(targetWidth, targetHeight), SKSamplingOptions.Default);
        using SKImage image = SKImage.FromBitmap(resized);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        using FileStream output = System.IO.File.Create(cacheFileName);
        encoded.SaveTo(output);
    }
}
