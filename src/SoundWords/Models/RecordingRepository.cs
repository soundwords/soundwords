using System.Collections.Generic;
using System.Data;
using System.Linq;
using MoreLinq;
using ServiceStack.Logging;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace SoundWords.Models
{ 
    public class RecordingRepository : IRecordingRepository
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILog _logger;

        public RecordingRepository(ILogFactory logFactory, IDbConnectionFactory dbConnectionFactory)
        {
            _logger = logFactory.GetLogger(GetType());
            _dbConnectionFactory = dbConnectionFactory;
        }

        public List<Recording> GetAllRecordings(bool includeRestricted)
        {
            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                _logger.Debug("Getting all recordings");
                if (includeRestricted)
                {
                    return db.Select<DbRecording>(x => !x.Deleted).ConvertAll(r => r.ToRecording());
                }

                return db.Select<DbRecording>(x => !x.Deleted && !x.Restricted).ConvertAll(r => r.ToRecording());
            }
        }

        public List<AlbumWithSpeakers> GetLatestAlbums(bool includeRestricted, int limit = 10)
        {
            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                _logger.Debug("Getting latest recordings");

                    var albumQuery = db.From<DbAlbum>()
                    .OrderByDescending(a => a.CreatedOn)
                    .Take(limit);
                if (!includeRestricted)
                {
                    albumQuery.Where(a => !a.Restricted);
                }

                var albumIdQuery =
                    albumQuery.Clone().Select(a => a.Id);

                var recordingIdQuery = db.From<DbRecording>()
                    .Where(r => Sql.In(r.AlbumId, albumIdQuery))
                    .Select(r => r.Id);

                var speakerQuery = db.From<DbSpeaker>()
                                     .Join<DbRecordingSpeaker>((speaker, recordingSpeaker) => speaker.Id == recordingSpeaker.SpeakerId)
                                     .Join<DbRecordingSpeaker, DbRecording>((recordingSpeaker, recording) => recordingSpeaker.RecordingId == recording.Id)
                                     .Join<DbRecording, DbAlbum>((recording, album) => recording.AlbumId == album.Id)
                                     .Where<DbSpeaker, DbRecordingSpeaker, DbAlbum, DbRecording>((speaker, recordingSpeaker, album, recording) =>
                                                                                                     !recording.Deleted &&
                                                                                                     !album.Deleted &&
                                                                                                     !speaker.Deleted &&
                                                                                                     Sql.In(recordingSpeaker.RecordingId, recordingIdQuery));

                var recordingsByAlbum = db.SelectMulti<DbSpeaker, DbRecordingSpeaker, DbRecording, DbAlbum>(speakerQuery)
                    .ToLookup(m => m.Item4.Id);

                var albums = db.Select(albumQuery);

                return albums.Select(a => new AlbumWithSpeakers
                {
                    Album = a.ToAlbum(),
                    Speakers = recordingsByAlbum[a.Id].Select(s => s.Item1).DistinctBy(s => s.Id).ToList().ConvertAll(s => s.ToSpeaker())
                }).ToList();
            }
        }

        public List<Speaker> GetSpeakers(bool includeRestricted)
        {
            _logger.Debug("Getting speaker list for right menu");

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                var filterExpression = db.From<DbRecording>()
                        .Join<DbRecordingSpeaker>((recording, recordingSpeaker) => recording.Id == recordingSpeaker.RecordingId)
                        .Join<DbRecordingSpeaker, DbSpeaker>((recordingSpeaker, speaker) => recordingSpeaker.SpeakerId == speaker.Id)
                        .Where<DbRecording, DbSpeaker>((recording, speaker) => !recording.Deleted && !speaker.Deleted);

                if (!includeRestricted)
                {
                    filterExpression.And(r => !r.Restricted);
                }
                var recordings = db.SelectMulti<DbRecording, DbRecordingSpeaker, DbSpeaker>(filterExpression);

                IEnumerable<DbSpeaker> speakers = recordings.Select(r => r.Item3).DistinctBy(s => s.Id);

                return speakers
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .Select(s => s.ToSpeaker())
                    .ToList();
            }
        }


        public Recording GetById(string uid)
        {
            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                return db.Single<DbRecording>(r => r.Uid == uid && !r.Deleted).ToRecording();
            }
        }
    }

    public class AlbumWithSpeakers
    {
        public Album Album { get; set; }
        public List<Speaker> Speakers { get; set; }
    }
}