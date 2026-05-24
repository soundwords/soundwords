using LinqToDB.Mapping;

namespace SoundWords.Models;

[Table("DbSpeaker", IsColumnAttributeRequired = false)]
public class DbSpeaker : DbEntity
{
    public string Uid { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public ushort? BirthDay { get; set; }
    public ushort? BirthMonth { get; set; }
    public ushort? BirthYear { get; set; }
    public string? Nationality { get; set; }
    public string? Description { get; set; }
    public string? PhotoPath { get; set; }
}
