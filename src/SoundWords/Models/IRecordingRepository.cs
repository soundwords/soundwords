namespace SoundWords.Models;

public interface IRecordingRepository
{
    List<Recording> GetAllRecordings(bool includeRestricted);
    Recording? GetById(string uid);
    List<Speaker> GetSpeakers(bool includeRestricted);
    List<AlbumWithSpeakers> GetLatestAlbums(bool includeRestricted, int limit = 10);
}
