using LinqToDB.Mapping;

namespace SoundWords.Models;

public class DbEntity
{
    [PrimaryKey, Identity]
    public long Id { get; set; }

    [Column]
    public DateTime CreatedOn { get; set; }

    [Column]
    public DateTime ModifiedOn { get; set; }

    [Column]
    public bool Deleted { get; set; }
}
