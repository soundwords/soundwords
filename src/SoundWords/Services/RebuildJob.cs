using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using MarkdownDeep;
using ServiceStack;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using SoundWords.Models;
using SoundWords.Tools;
using File = TagLib.File;

namespace SoundWords.Services
{
    internal class RebuildJob : IJob<SubscriptionInfo>
    {
        private const string SoundWordsRecordingIdField = "SoundWords Recording ID";
        private const string SoundWordsAlbumIdField = "SoundWords Album ID";
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly Func<string, bool, File.IFileAbstraction> _fileAbstractionFactory;
        private readonly IFileSystem _fileSystem;
        private readonly ILog _logger;
        private readonly IServerEvents _serverEvents;
        private readonly ISoundWordsConfiguration _soundWordsConfiguration;

        public RebuildJob(ILogFactory logFactory, ISoundWordsConfiguration soundWordsConfiguration, IDbConnectionFactory dbConnectionFactory,
                          IServerEvents serverEvents, IFileSystem fileSystem,
                          Func<string, bool, File.IFileAbstraction> fileAbstractionFactory)
        {
            _logger = logFactory.GetLogger(GetType());
            _soundWordsConfiguration = soundWordsConfiguration;
            _dbConnectionFactory = dbConnectionFactory;
            _serverEvents = serverEvents;
            _fileSystem = fileSystem;
            _fileAbstractionFactory = fileAbstractionFactory;
        }

        public Task Execute(SubscriptionInfo subscriptionInfo)
        {
            return Task.Run(() =>
                            {
                                PublishProgress(subscriptionInfo, 0);
                                PublishStatus(subscriptionInfo, new Status("processing", "Rebuild: Processing public"));
                                RebuildFromFolder(subscriptionInfo, _soundWordsConfiguration.RecordingsFolder, false, 0, 50);

                                PublishStatus(subscriptionInfo, new Status("processing", "Rebuild: Processing restricted"));
                                RebuildFromFolder(subscriptionInfo, _soundWordsConfiguration.RestrictedRecordingsFolder, true, 50, 80);

                                PublishStatus(subscriptionInfo, new Status("processing", "Rebuild: Scanning for speaker photos, etc."));
                                ScanForSpeakerInfo(subscriptionInfo, _soundWordsConfiguration.RestrictedRecordingsFolder, true, 85, 90);
                                ScanForSpeakerInfo(subscriptionInfo, _soundWordsConfiguration.RecordingsFolder, false, 80, 85);

                                PublishStatus(subscriptionInfo, new Status("processing", "Rebuild: Processing deleted"));
                                Prune();
                                PublishProgress(subscriptionInfo, 100);
                                PublishStatus(subscriptionInfo, new Status("finished", "Finished"));
                            }).ContinueWith(task =>
                                            {
                                                if (task.Exception != null)
                                                {
                                                    AggregateException aggregateException = task.Exception.Flatten();
                                                    PublishStatus(subscriptionInfo, new Status("error", $"An error occurred: {aggregateException}"));
                                                    _logger.Error("An error occurred", aggregateException);
                                                }
                                            });
        }

        private void PublishStatus(SubscriptionInfo subscriptionInfo, Status status)
        {
            _logger.DebugFormat("Notifying: {0}", status.Text);
            _serverEvents.NotifySubscription(subscriptionInfo.SubscriptionId, "cmd.status", status);
        }

        private void PublishProgress(SubscriptionInfo subscriptionInfo, int progress)
        {
            _logger.DebugFormat("Progress: {0}", progress);
            _serverEvents.NotifySubscription(subscriptionInfo.SubscriptionId, "cmd.progress", new {Progress = progress});
        }

        private void RebuildFromFolder(SubscriptionInfo subscriptionInfo, string folderPath, bool restricted, int startPercent = 0, int endPercent = 100)
        {
            if (folderPath == null)
            {
                throw new ArgumentNullException(nameof(folderPath));
            }

            using (IDbConnection db = _dbConnectionFactory.Open())
            {
                IDirectoryInfo directoryInfo = _fileSystem.DirectoryInfo.New(folderPath);
                IFileInfo[] files = directoryInfo.GetFiles("*.mp3", SearchOption.AllDirectories);

                int index = 0;
                foreach (IGrouping<string, IFileInfo> albumFiles in files.GroupBy(f => f.DirectoryName))
                {
                    using (IDbTransaction transaction = db.OpenTransaction())
                    {
                        _logger.Debug("Fetching album");
                        DbAlbum album = db.Single<DbAlbum>(a => a.Path == albumFiles.Key);

                        foreach (IFileInfo file in albumFiles)
                        {
                            int progress =
                                (int) Math.Round((index + 1) / (double) files.Length * (endPercent - startPercent)) +
                                startPercent;
                            PublishProgress(subscriptionInfo, progress);
                            _logger.DebugFormat("Processing file {0}", file.FullName);
                            DbRecording recording;

                            bool saveTagFile = false;

                            _logger.Debug("Reading tag file");
                            using (File tagFile = File.Create(_fileAbstractionFactory(file.FullName, true)))
                            {
                                string recordingId = tagFile.GetCustomField(SoundWordsRecordingIdField);
                                if (recordingId != null)
                                {
                                    _logger.Debug("Fetching recording");
                                    recording = db.Single<DbRecording>(r => r.Uid == recordingId) ?? new DbRecording {Uid = recordingId};
                                }
                                else
                                {
                                    recording = new DbRecording {Uid = Guid.NewGuid().ToString("N")};
                                    saveTagFile = true;
                                    tagFile.SetCustomField(SoundWordsRecordingIdField, recording.Uid);
                                }

                                if (album == null)
                                {
                                    string uid = tagFile.GetCustomField(SoundWordsAlbumIdField) ?? Guid.NewGuid().ToString("N");

                                    album = new DbAlbum
                                            {
                                                Uid = uid,
                                                Name = (tagFile.Tag.Album ?? "Ukjent").Trim(),
                                                Path = albumFiles.Key,
                                                Restricted = restricted
                                            };
                                }

                                UpdateAttachments(albumFiles.Key, album);

                                if (album.Id != 0)
                                {
                                    _logger.Debug("Saving album");
                                    db.Update(album);
                                }
                                else
                                {
                                    _logger.Debug("Creating album");
                                    album.Id = db.Insert(album, true);
                                }

                                if (tagFile.GetCustomField(SoundWordsAlbumIdField) != album.Uid)
                                {
                                    tagFile.SetCustomField(SoundWordsAlbumIdField, album.Uid);
                                    saveTagFile = true;
                                }

                                recording.AlbumId = album.Id;
                                recording.Title = (tagFile.Tag.Title ?? "Ukjent").Trim();
                                recording.Track = (ushort) tagFile.Tag.Track;
                                recording.Comment = tagFile.Tag.Comment;
                                recording.Year = tagFile.Tag.Year != 0 ? (ushort?) tagFile.Tag.Year : null;
                                recording.Path = file.FullName;
                                recording.Restricted = restricted;

                                if (recording.Id == 0)
                                {
                                    _logger.DebugFormat("Creating recording: {0}", recording.Dump());
                                    recording.Id = db.Insert(recording, true);
                                }
                                else
                                {
                                    _logger.DebugFormat("Saving recording: {0}", recording.Dump());
                                    db.Update(recording);
                                }

                                db.Delete<DbRecordingSpeaker>(rs => rs.RecordingId == recording.Id);

                                foreach (string performer in tagFile.Tag.Performers)
                                {
                                    _logger.DebugFormat($"Creating speaker {performer}");
                                    NameInfo nameInfo = performer.ToNameInfo();
                                    DbSpeaker speaker = db.Single<DbSpeaker>(s =>
                                                                                 s.FirstName == nameInfo.FirstName && s.LastName == nameInfo.LastName);

                                    if (speaker == null)
                                    {
                                        speaker = new DbSpeaker
                                                  {
                                                      Uid = Guid.NewGuid().ToString("N"),
                                                      FirstName = nameInfo.FirstName,
                                                      LastName = nameInfo.LastName
                                                  };

                                        speaker.Id = db.Insert(speaker, true);
                                    }

                                    if (!db.Exists<DbRecordingSpeaker>(rs =>
                                                                           rs.RecordingId == recording.Id && rs.SpeakerId == speaker.Id))
                                    {
                                        db.Insert(new DbRecordingSpeaker
                                                  {
                                                      RecordingId = recording.Id,
                                                      SpeakerId = speaker.Id
                                                  });
                                    }
                                }

                                if (saveTagFile)
                                {
                                    _logger.Debug("Writing ID tag data");
                                    tagFile.Save();
                                }
                            }

                            index++;
                        }

                        _logger.Info("Committing transaction");
                        transaction.Commit();
                    }
                }
            }
        }

        private void ScanForSpeakerInfo(SubscriptionInfo subscriptionInfo, string recordingsFolder, bool restricted, int progressFrom, int progressTo)
        {
            ILookup<string, IDirectoryInfo> directories = _fileSystem.DirectoryInfo.New(recordingsFolder)
                                                                        .GetDirectories("*", SearchOption.AllDirectories)
                                                                        .ToLookup(d => d.Name);

            using (IDbConnection db = _dbConnectionFactory.Open())
            using (IDbTransaction transaction = db.OpenTransaction())
            {
                List<DbSpeaker> speakers = db.Select<DbSpeaker>();
                for (int index = 0; index < speakers.Count; index++)
                {
                    DbSpeaker speaker = speakers[index];

                    string fullName = speaker.ToSpeaker().FullName;
                    if (directories.Contains(fullName))
                    {
                        IFileInfo photo = directories[fullName]
                            .SelectMany(d => d.GetFiles("*.jpg"))
                            .OrderByDescending(f => f.Length)
                            .FirstOrDefault();

                        if (photo != null)
                        {
                            speaker.PhotoPath = photo.FullName;
                            db.Update(speaker);
                        }

                        IFileInfo markdownFile = directories[fullName]
                            .SelectMany(d => d.GetFiles("*.md"))
                            .OrderByDescending(f => f.Length)
                            .FirstOrDefault();

                        if (markdownFile != null)
                        {
                            string markdownText = _fileSystem.File.ReadAllText(markdownFile.FullName);
                            speaker.Description = new Markdown().Transform(markdownText);
                        }

                        if (markdownFile != null || photo != null)
                        {
                            db.Update(speaker);
                        }
                    }

                    PublishProgress(subscriptionInfo, (progressTo - progressFrom) * (index / speakers.Count));
                }

                transaction.Commit();
            }
        }

        private void Prune()
        {
            _logger.Info("Pruning deleted stuff");
            using (IDbConnection db = _dbConnectionFactory.Open())
            using (IDbTransaction transaction = db.OpenTransaction())
            {
                _logger.Debug("Pruning recordings");
                List<DbRecording> recordings = db.Select<DbRecording>();
                foreach (DbRecording recording in recordings)
                {
                    if (!_fileSystem.File.Exists(recording.Path))
                    {
                        _logger.DebugFormat("Deleting deleted recording {0}", recording.Title);
                        recording.Deleted = true;
                        db.Update(recording);
                    }
                }

                transaction.Commit();
            }

            using (IDbConnection db = _dbConnectionFactory.Open())
            using (IDbTransaction transaction = db.OpenTransaction())
            {
                _logger.Debug("Pruning albums");

                List<DbAlbum> emptyAlbums = db.Select(db.From<DbAlbum>()
                                                        .Where(a => !Sql.In(a.Id, db.From<DbRecording>().SelectDistinct(r => r.AlbumId))));
                foreach (DbAlbum album in emptyAlbums)
                {
                    _logger.DebugFormat("Deleting album {0}", album.Name);
                    album.Deleted = true;
                    db.Update(album);
                }

                transaction.Commit();
            }
        }

        private void UpdateAttachments(string directoryName, DbAlbum album)
        {
            _logger.Debug("Updating attachments");
            string imagePath = _fileSystem.Path.Combine(directoryName, "folder.jpg");
            if (_fileSystem.File.Exists(imagePath))
            {
                album.AlbumArtPath = imagePath;
            }

            string markdownPath = _fileSystem.Directory.EnumerateFiles(directoryName, "*.md").FirstOrDefault();
            if (markdownPath != null)
            {
                string markdownText = _fileSystem.File.ReadAllText(markdownPath);
                album.Description = new Markdown().Transform(markdownText);
            }

            album.AttachmentPaths = _fileSystem.Directory.GetFiles(directoryName).Where(f =>
                                                                                            !f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                                                                            && !f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)
                                                                                            && !f.EndsWith("folder.jpg", StringComparison.OrdinalIgnoreCase)
                                                                                            && !f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                                               .ToList();
        }
    }
}
