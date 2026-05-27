using System.IO.Abstractions;
using LinqToDB;
using Markdig;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SoundWords.Data;
using SoundWords.Hubs;
using SoundWords.Models;
using File = TagLib.File;

namespace SoundWords.Tools;

public class RebuildJob : IJob<string>
{
    private const string SoundWordsRecordingIdField = "SoundWords Recording ID";
    private const string SoundWordsAlbumIdField = "SoundWords Album ID";

    private readonly Func<SoundWordsDb> _dbFactory;
    private readonly Func<string, bool, File.IFileAbstraction> _fileAbstractionFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IHubContext<RebuildHub> _hub;
    private readonly ILogger<RebuildJob> _logger;
    private readonly ISoundWordsConfiguration _soundWordsConfiguration;

    public RebuildJob(ILogger<RebuildJob> logger,
                      ISoundWordsConfiguration soundWordsConfiguration,
                      Func<SoundWordsDb> dbFactory,
                      IHubContext<RebuildHub> hub,
                      IFileSystem fileSystem,
                      Func<string, bool, File.IFileAbstraction> fileAbstractionFactory)
    {
        _logger = logger;
        _soundWordsConfiguration = soundWordsConfiguration;
        _dbFactory = dbFactory;
        _hub = hub;
        _fileSystem = fileSystem;
        _fileAbstractionFactory = fileAbstractionFactory;
    }

    public Task Execute(string jobId)
    {
        return Task.Run(async () =>
                        {
                            await PublishProgress(jobId, 0);
                            await PublishStatus(jobId, new RebuildStatus("processing", "Rebuild: Processing public"));
                            await RebuildFromFolder(jobId, _soundWordsConfiguration.RecordingsFolder, false, 0, 50);

                            await PublishStatus(jobId, new RebuildStatus("processing", "Rebuild: Processing restricted"));
                            await RebuildFromFolder(jobId, _soundWordsConfiguration.RestrictedRecordingsFolder, true, 50, 80);

                            await PublishStatus(jobId, new RebuildStatus("processing", "Rebuild: Scanning for speaker photos, etc."));
                            await ScanForSpeakerInfo(jobId, _soundWordsConfiguration.RestrictedRecordingsFolder, 85, 90);
                            await ScanForSpeakerInfo(jobId, _soundWordsConfiguration.RecordingsFolder, 80, 85);

                            await PublishStatus(jobId, new RebuildStatus("processing", "Rebuild: Processing deleted"));
                            await Prune();
                            await PublishProgress(jobId, 100);
                            await PublishStatus(jobId, new RebuildStatus("finished", "Finished"));
                        }).ContinueWith(async task =>
                                        {
                                            if (task.Exception != null)
                                            {
                                                _logger.LogError(task.Exception.Flatten(),
                                                                 "Rebuild job {JobId} failed", jobId);
                                                await PublishStatus(jobId, new RebuildStatus("error",
                                                                                             "Rebuild failed. See server logs for details."));
                                            }
                                        });
    }

    private Task PublishStatus(string jobId, RebuildStatus status)
    {
        _logger.LogDebug("Notifying: {Status}", status.Text);
        return _hub.Clients.Group(jobId).SendAsync("status", status);
    }

    private Task PublishProgress(string jobId, int progress)
    {
        _logger.LogDebug("Progress: {Progress}", progress);
        return _hub.Clients.Group(jobId).SendAsync("progress", new RebuildProgress(progress));
    }

    private async Task RebuildFromFolder(string jobId, string folderPath, bool restricted, int startPercent, int endPercent)
    {
        if (folderPath == null)
        {
            throw new ArgumentNullException(nameof(folderPath));
        }

        using SoundWordsDb db = _dbFactory();
        IDirectoryInfo directoryInfo = _fileSystem.DirectoryInfo.New(folderPath);
        IFileInfo[] files = directoryInfo.GetFiles("*.mp3", SearchOption.AllDirectories);

        int index = 0;
        foreach (IGrouping<string?, IFileInfo> albumFiles in files.GroupBy(f => f.DirectoryName))
        {
            await using LinqToDB.Data.DataConnectionTransaction transaction = await db.BeginTransactionAsync();
            _logger.LogDebug("Fetching album");
            DbAlbum? album = db.Albums.SingleOrDefault(a => a.Path == albumFiles.Key);

            foreach (IFileInfo file in albumFiles)
            {
                int progress = (int)Math.Round((index + 1) / (double)files.Length * (endPercent - startPercent)) + startPercent;
                await PublishProgress(jobId, progress);
                _logger.LogDebug("Processing file {Path}", file.FullName);

                bool saveTagFile = false;
                using File tagFile = File.Create(_fileAbstractionFactory(file.FullName, true));

                string? recordingId = tagFile.GetCustomField(SoundWordsRecordingIdField);
                DbRecording recording;
                if (recordingId != null)
                {
                    recording = db.Recordings.SingleOrDefault(r => r.Uid == recordingId)
                                ?? new DbRecording { Uid = recordingId };
                }
                else
                {
                    recording = new DbRecording { Uid = Guid.NewGuid().ToString("N") };
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

                UpdateAttachments(albumFiles.Key!, album);

                if (album.Id != 0)
                {
                    _logger.LogDebug("Saving album");
                    await db.UpdateWithTimestampAsync(album);
                }
                else
                {
                    _logger.LogDebug("Creating album");
                    album.Id = await db.InsertWithTimestampsAsync(album);
                }

                if (tagFile.GetCustomField(SoundWordsAlbumIdField) != album.Uid)
                {
                    tagFile.SetCustomField(SoundWordsAlbumIdField, album.Uid);
                    saveTagFile = true;
                }

                recording.AlbumId = album.Id;
                recording.Title = (tagFile.Tag.Title ?? "Ukjent").Trim();
                recording.Track = (ushort)tagFile.Tag.Track;
                recording.Comment = tagFile.Tag.Comment;
                recording.Year = tagFile.Tag.Year != 0 ? (ushort?)tagFile.Tag.Year : null;
                recording.Path = file.FullName;
                recording.Restricted = restricted;

                if (recording.Id == 0)
                {
                    _logger.LogDebug("Creating recording {Title}", recording.Title);
                    recording.Id = await db.InsertWithTimestampsAsync(recording);
                }
                else
                {
                    _logger.LogDebug("Saving recording {Title}", recording.Title);
                    await db.UpdateWithTimestampAsync(recording);
                }

                await db.RecordingSpeakers.Where(rs => rs.RecordingId == recording.Id).DeleteAsync();

                foreach (string performer in tagFile.Tag.Performers)
                {
                    _logger.LogDebug("Processing speaker {Performer}", performer);
                    NameInfo nameInfo = performer.ToNameInfo();
                    DbSpeaker? speaker = db.Speakers.SingleOrDefault(s =>
                                                                        s.FirstName == nameInfo.FirstName
                                                                                        && s.LastName == nameInfo.LastName);

                    if (speaker == null)
                    {
                        speaker = new DbSpeaker
                                  {
                                      Uid = Guid.NewGuid().ToString("N"),
                                      FirstName = nameInfo.FirstName,
                                      LastName = nameInfo.LastName
                                  };
                        speaker.Id = await db.InsertWithTimestampsAsync(speaker);
                    }

                    if (!db.RecordingSpeakers.Any(rs => rs.RecordingId == recording.Id && rs.SpeakerId == speaker.Id))
                    {
                        DateTime now = DateTime.UtcNow;
                        await db.InsertAsync(new DbRecordingSpeaker
                                             {
                                                 RecordingId = recording.Id,
                                                 SpeakerId = speaker.Id,
                                                 CreatedOn = now,
                                                 ModifiedOn = now
                                             });
                    }
                }

                if (saveTagFile)
                {
                    _logger.LogDebug("Writing ID tag data");
                    tagFile.Save();
                }

                index++;
            }

            _logger.LogInformation("Committing transaction");
            await transaction.CommitAsync();
        }
    }

    private async Task ScanForSpeakerInfo(string jobId, string recordingsFolder, int progressFrom, int progressTo)
    {
        ILookup<string, IDirectoryInfo> directories = _fileSystem.DirectoryInfo.New(recordingsFolder)
                                                                 .GetDirectories("*", SearchOption.AllDirectories)
                                                                 .ToLookup(d => d.Name);

        using SoundWordsDb db = _dbFactory();
        await using LinqToDB.Data.DataConnectionTransaction transaction = await db.BeginTransactionAsync();
        List<DbSpeaker> speakers = db.Speakers.ToList();
        for (int index = 0; index < speakers.Count; index++)
        {
            DbSpeaker speaker = speakers[index];
            string fullName = speaker.ToSpeaker().FullName;
            if (directories.Contains(fullName))
            {
                IFileInfo? photo = directories[fullName]
                                   .SelectMany(d => d.GetFiles("*.jpg"))
                                   .OrderByDescending(f => f.Length)
                                   .FirstOrDefault();

                bool dirty = false;
                if (photo != null)
                {
                    speaker.PhotoPath = photo.FullName;
                    dirty = true;
                }

                IFileInfo? markdownFile = directories[fullName]
                                          .SelectMany(d => d.GetFiles("*.md"))
                                          .OrderByDescending(f => f.Length)
                                          .FirstOrDefault();
                if (markdownFile != null)
                {
                    string markdownText = _fileSystem.File.ReadAllText(markdownFile.FullName);
                    speaker.Description = Markdown.ToHtml(markdownText);
                    dirty = true;
                }

                if (dirty)
                {
                    await db.UpdateWithTimestampAsync(speaker);
                }
            }

            await PublishProgress(jobId, (progressTo - progressFrom) * (index / Math.Max(speakers.Count, 1)));
        }

        await transaction.CommitAsync();
    }

    private async Task Prune()
    {
        _logger.LogInformation("Pruning deleted stuff");
        using (SoundWordsDb db = _dbFactory())
        await using (LinqToDB.Data.DataConnectionTransaction transaction = await db.BeginTransactionAsync())
        {
            _logger.LogDebug("Pruning recordings");
            List<DbRecording> recordings = db.Recordings.ToList();
            foreach (DbRecording recording in recordings)
            {
                if (!_fileSystem.File.Exists(recording.Path))
                {
                    _logger.LogDebug("Deleting recording {Title}", recording.Title);
                    recording.Deleted = true;
                    await db.UpdateWithTimestampAsync(recording);
                }
            }

            await transaction.CommitAsync();
        }

        using (SoundWordsDb db = _dbFactory())
        await using (LinqToDB.Data.DataConnectionTransaction transaction = await db.BeginTransactionAsync())
        {
            _logger.LogDebug("Pruning albums");
            long[] inUseAlbumIds = db.Recordings.Select(r => r.AlbumId).Distinct().ToArray();
            List<DbAlbum> emptyAlbums = db.Albums.Where(a => !inUseAlbumIds.Contains(a.Id)).ToList();
            foreach (DbAlbum album in emptyAlbums)
            {
                _logger.LogDebug("Deleting album {Name}", album.Name);
                album.Deleted = true;
                await db.UpdateWithTimestampAsync(album);
            }

            await transaction.CommitAsync();
        }
    }

    private void UpdateAttachments(string directoryName, DbAlbum album)
    {
        _logger.LogDebug("Updating attachments");
        string imagePath = _fileSystem.Path.Combine(directoryName, "folder.jpg");
        if (_fileSystem.File.Exists(imagePath))
        {
            album.AlbumArtPath = imagePath;
        }

        string? markdownPath = _fileSystem.Directory.EnumerateFiles(directoryName, "*.md").FirstOrDefault();
        if (markdownPath != null)
        {
            string markdownText = _fileSystem.File.ReadAllText(markdownPath);
            album.Description = Markdown.ToHtml(markdownText);
        }

        album.AttachmentPaths = _fileSystem.Directory.GetFiles(directoryName)
                                           .Where(f =>
                                                      !f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                                                          && !f.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)
                                                                          && !f.EndsWith("folder.jpg", StringComparison.OrdinalIgnoreCase)
                                                                          && !f.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                                           .ToList();
    }
}
