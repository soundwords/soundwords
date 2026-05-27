using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using ServiceStack.Text;
using SoundWords.Models;

namespace SoundWords.Data;

public class SoundWordsDb : DataConnection
{
    private static readonly MappingSchema SharedSchema = BuildMappingSchema();

    public SoundWordsDb(DataOptions<SoundWordsDb> options)
        : base(options.Options.UseMappingSchema(SharedSchema))
    {
    }

    public ITable<DbAlbum> Albums => this.GetTable<DbAlbum>();
    public ITable<DbRecording> Recordings => this.GetTable<DbRecording>();
    public ITable<DbSpeaker> Speakers => this.GetTable<DbSpeaker>();
    public ITable<DbRecordingSpeaker> RecordingSpeakers => this.GetTable<DbRecordingSpeaker>();
    public ITable<DbScripture> Scriptures => this.GetTable<DbScripture>();

    public static string GetProvider(string dbType)
    {
        return dbType switch
        {
            "PostgreSQL" => ProviderName.PostgreSQL,
            "MySQL" => ProviderName.MySql80MySqlConnector,
            "SQLServer" => ProviderName.SqlServer,
            "SQLite" => ProviderName.SQLiteMS,
            _ => throw new ArgumentOutOfRangeException(nameof(dbType), "The database type is not supported")
        };
    }

    public async Task<long> InsertWithTimestampsAsync<T>(T entity) where T : DbEntity
    {
        DateTime now = DateTime.UtcNow;
        entity.CreatedOn = now;
        entity.ModifiedOn = now;
        return await this.InsertWithInt64IdentityAsync(entity);
    }

    public Task<int> UpdateWithTimestampAsync<T>(T entity) where T : DbEntity
    {
        entity.ModifiedOn = DateTime.UtcNow;
        return this.UpdateAsync(entity);
    }

    private static MappingSchema BuildMappingSchema()
    {
        MappingSchema schema = new();
        new FluentMappingBuilder(schema)
            .Entity<DbAlbum>()
            .Property(e => e.AttachmentPaths)
            .HasConversion(v => SerializeStringList(v), v => DeserializeStringList(v))
            .Build();
        return schema;
    }

    /// <summary>
    /// Serialises <see cref="List{String}"/> columns in ServiceStack JSV format,
    /// matching what the legacy app produces so both apps can read each other's
    /// writes during the beta cutover period.
    /// </summary>
    internal static string? SerializeStringList(List<string>? value)
    {
        return value == null ? null : TypeSerializer.SerializeToString(value);
    }

    internal static List<string>? DeserializeStringList(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : TypeSerializer.DeserializeFromString<List<string>>(value);
    }
}
