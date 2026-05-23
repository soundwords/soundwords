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
        schema.SetConverter<List<string>?, string?>(SerializeStringList);
        schema.SetConverter<string?, List<string>?>(DeserializeStringList);
        return schema;
    }

    /// <summary>
    /// Serialises <see cref="List{String}"/> columns in ServiceStack JSV format,
    /// matching what the legacy app produces, so both apps can read each other's
    /// writes during the beta cutover period.
    /// </summary>
    internal static string? SerializeStringList(List<string>? value)
    {
        return value == null ? null : TypeSerializer.SerializeToString(value);
    }

    /// <summary>
    /// Reads a <see cref="List{String}"/> stored as either JSV (legacy + current
    /// writes) or JSON (briefly written during the rewrite). JSON is tried first
    /// because System.Text.Json is strict and fails fast on JSV input.
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
