using LinqToDB;
using Microsoft.Extensions.Logging;
using SoundWords.Data;

namespace SoundWords.Models;

public class RecordingRepository : IRecordingRepository
{
    private readonly Func<SoundWordsDb> _dbFactory;
    private readonly ILogger<RecordingRepository> _logger;

    public RecordingRepository(ILogger<RecordingRepository> logger, Func<SoundWordsDb> dbFactory)
    {
        _logger = logger;
        _dbFactory = dbFactory;
    }

    public List<Recording> GetAllRecordings(bool includeRestricted)
    {
        using SoundWordsDb db = _dbFactory();
        _logger.LogDebug("Getting all recordings");

        IQueryable<DbRecording> query = db.Recordings.Where(x => !x.Deleted);
        if (!includeRestricted)
        {
            query = query.Where(x => !x.Restricted);
        }

        return query.ToList().ConvertAll(r => r.ToRecording());
    }

    public List<AlbumWithSpeakers> GetLatestAlbums(bool includeRestricted, int limit = 10)
    {
        using SoundWordsDb db = _dbFactory();
        _logger.LogDebug("Getting latest recordings");

        IQueryable<DbAlbum> latestAlbumsQuery = db.Albums.Where(a => !a.Deleted)
                                                  .OrderByDescending(a => a.CreatedOn);
        if (!includeRestricted)
        {
            latestAlbumsQuery = latestAlbumsQuery.Where(a => !a.Restricted);
        }
        latestAlbumsQuery = latestAlbumsQuery.Take(limit);

        List<DbAlbum> albums = latestAlbumsQuery.ToList();
        IQueryable<long> latestAlbumIds = latestAlbumsQuery.Select(a => a.Id);

        var albumSpeakers = (from speaker in db.Speakers
                             join recordingSpeaker in db.RecordingSpeakers on speaker.Id equals recordingSpeaker.SpeakerId
                             join recording in db.Recordings on recordingSpeaker.RecordingId equals recording.Id
                             join album in db.Albums on recording.AlbumId equals album.Id
                             where !speaker.Deleted && !recording.Deleted && !album.Deleted
                                                    && latestAlbumIds.Contains(album.Id)
                             select new { AlbumId = album.Id, Speaker = speaker })
                            .ToList()
                            .ToLookup(x => x.AlbumId, x => x.Speaker);

        return albums.Select(a => new AlbumWithSpeakers
                                  {
                                      Album = a.ToAlbum(),
                                      Speakers = albumSpeakers[a.Id]
                                                 .DistinctBy(s => s.Id)
                                                 .Select(s => s.ToSpeaker())
                                                 .ToList()
                                  }).ToList();
    }

    public List<Speaker> GetSpeakers(bool includeRestricted)
    {
        _logger.LogDebug("Getting speaker list for right menu");

        using SoundWordsDb db = _dbFactory();

        var query = from speaker in db.Speakers
                    join recordingSpeaker in db.RecordingSpeakers on speaker.Id equals recordingSpeaker.SpeakerId
                    join recording in db.Recordings on recordingSpeaker.RecordingId equals recording.Id
                    where !speaker.Deleted && !recording.Deleted
                    select new { speaker, recording };

        if (!includeRestricted)
        {
            query = query.Where(x => !x.recording.Restricted);
        }

        return query.Select(x => x.speaker).ToList()
                    .DistinctBy(s => s.Id)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => s.ToSpeaker())
                    .ToList();
    }

    public Recording? GetById(string uid)
    {
        using SoundWordsDb db = _dbFactory();
        DbRecording? dbRecording = db.Recordings.SingleOrDefault(r => r.Uid == uid && !r.Deleted);
        return dbRecording?.ToRecording();
    }
}

public class AlbumWithSpeakers
{
    public Album Album { get; set; } = null!;
    public List<Speaker> Speakers { get; set; } = new();
}
