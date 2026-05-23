using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using ServiceStack.Text;
using SoundWords.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;
using JsonException = System.Text.Json.JsonException;

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
            "MySQL" => ProviderName.MySqlConnector,
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
        schema.SetConverter<List<string>?, string?>(list => list == null ? null : JsonSerializer.Serialize(list));
        schema.SetConverter<string?, List<string>?>(DeserializeStringList);
        return schema;
    }

    /// <summary>
    /// Reads a <see cref="List{String}"/> stored either as JSON (current format)
    /// or as ServiceStack JSV (legacy format from before the rewrite). New writes
    /// always use JSON, so rows migrate to JSON on the next save.
    /// </summary>
    internal static List<string>? DeserializeStringList(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(value);
        }
        catch (JsonException)
        {
            return TypeSerializer.DeserializeFromString<List<string>>(value);
        }
    }
}
