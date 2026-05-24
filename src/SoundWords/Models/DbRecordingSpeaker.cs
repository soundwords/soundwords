using LinqToDB.Mapping;

namespace SoundWords.Models;

[Table("DbRecordingSpeaker", IsColumnAttributeRequired = false)]
public class DbRecordingSpeaker
{
    [Column, PrimaryKey(0)]
    public long RecordingId { get; set; }

    [Column, PrimaryKey(1)]
    public long SpeakerId { get; set; }

    [Column]
    public DateTime CreatedOn { get; set; }

    [Column]
    public DateTime ModifiedOn { get; set; }
}
