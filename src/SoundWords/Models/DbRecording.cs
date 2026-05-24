using LinqToDB.Mapping;

namespace SoundWords.Models;

[Table("DbRecording", IsColumnAttributeRequired = false)]
public class DbRecording : DbEntity
{
    public string Uid { get; set; } = null!;

    public long AlbumId { get; set; }

    public string? Title { get; set; }
    public ushort Track { get; set; }
    public string? Comment { get; set; }
    public ushort? Day { get; set; }
    public ushort? Month { get; set; }
    public ushort? Year { get; set; }
    public string? Path { get; set; }
    public bool Restricted { get; set; }

    [Association(ThisKey = nameof(AlbumId), OtherKey = nameof(DbAlbum.Id), CanBeNull = false)]
    public DbAlbum Album { get; set; } = null!;
}
