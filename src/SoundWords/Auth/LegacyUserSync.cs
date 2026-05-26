using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SoundWords.Data;

namespace SoundWords.Auth;

public interface ILegacyUserSync
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// One-way reconciliation from the legacy ServiceStack <c>UserAuth</c> table
/// into Identity's <c>AspNetUsers</c>. Runs at every startup so the new app
/// keeps picking up registrations and edits made on the still-live old app
/// during the beta period.
/// </summary>
/// <remarks>
/// Conflict rules:
/// <list type="bullet">
///   <item><description>New legacy user → insert.</description></item>
///   <item><description>Name fields differ → overwrite (last-write-wins, legacy is source of truth).</description></item>
///   <item><description>Password hash → only overwritten while the AspNetUsers row is still in legacy SaltedHash form (PasswordHash starts with <c>SS$</c>). Once <see cref="LegacyAwarePasswordHasher"/> has upgraded the hash to Identity v3 on first login, the legacy column can no longer overwrite it.</description></item>
/// </list>
/// </remarks>
public sealed class LegacyUserSync : ILegacyUserSync
{
    private const string LegacyHashPrefix = "SS$";
    private readonly IConfiguration _configuration;
    private readonly ILogger<LegacyUserSync> _logger;

    public LegacyUserSync(IConfiguration configuration, ILogger<LegacyUserSync> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        string? dbType = _configuration["DB_TYPE"];
        string? usersConnection = _configuration["CONNECTION_STRING_USERS"]
                                  ?? _configuration["CONNECTION_STRING"];
        if (dbType == null || usersConnection == null)
        {
            return;
        }

        using DataConnection db = new(new DataOptions().UseConnectionString(SoundWordsDb.GetProvider(dbType), usersConnection));

        List<LegacyUserAuthRow> legacyUsers;
        try
        {
            legacyUsers = await db.GetTable<LegacyUserAuthRow>()
                                  .Where(u => u.Email != null)
                                  .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Legacy user sync skipped — UserAuth table not present.");
            _logger.LogDebug(ex, "Legacy user sync underlying exception.");
            return;
        }

        Dictionary<string, IdentityUserRow> existingByNormalizedEmail =
            await db.GetTable<IdentityUserRow>()
                    .Where(u => u.NormalizedEmail != null)
                    .ToDictionaryAsync(u => u.NormalizedEmail!, cancellationToken);

        int created = 0;
        int updated = 0;
        int unchanged = 0;

        foreach (LegacyUserAuthRow legacy in legacyUsers)
        {
            string normalizedEmail = legacy.Email!.ToUpperInvariant();
            string legacyHash = ComposeLegacyHash(legacy);

            if (!existingByNormalizedEmail.TryGetValue(normalizedEmail, out IdentityUserRow? existing))
            {
                await InsertAsync(db, legacy, normalizedEmail, legacyHash, cancellationToken);
                created++;
                continue;
            }

            bool legacyHashStillCurrent =
                existing.PasswordHash?.StartsWith(LegacyHashPrefix, StringComparison.Ordinal) == true;
            bool passwordChanged = legacyHashStillCurrent && existing.PasswordHash != legacyHash;
            bool nameChanged = existing.FirstName != legacy.FirstName
                            || existing.LastName != legacy.LastName
                            || existing.DisplayName != legacy.DisplayName;

            if (!nameChanged && !passwordChanged)
            {
                unchanged++;
                continue;
            }

            string newHash = passwordChanged ? legacyHash : existing.PasswordHash!;
            await db.GetTable<IdentityUserRow>()
                    .Where(u => u.Id == existing.Id)
                    .Set(u => u.FirstName, legacy.FirstName)
                    .Set(u => u.LastName, legacy.LastName)
                    .Set(u => u.DisplayName, legacy.DisplayName)
                    .Set(u => u.PasswordHash, newHash)
                    .UpdateAsync(cancellationToken);
            updated++;
        }

        _logger.LogInformation(
            "Legacy user sync: created {Created}, updated {Updated}, unchanged {Unchanged}, scanned {Total}",
            created, updated, unchanged, legacyUsers.Count);
    }

    private static async Task InsertAsync(DataConnection db, LegacyUserAuthRow legacy, string normalizedEmail,
                                          string legacyHash, CancellationToken cancellationToken)
    {
        string handle = string.IsNullOrEmpty(legacy.UserName) ? legacy.Email! : legacy.UserName!;
        IdentityUserRow row = new()
                              {
                                  Id = Guid.NewGuid().ToString("N"),
                                  UserName = handle.ToLowerInvariant(),
                                  NormalizedUserName = handle.ToUpperInvariant(),
                                  Email = legacy.Email,
                                  NormalizedEmail = normalizedEmail,
                                  EmailConfirmed = true,
                                  PasswordHash = legacyHash,
                                  SecurityStamp = Guid.NewGuid().ToString("N"),
                                  ConcurrencyStamp = Guid.NewGuid().ToString("N"),
                                  PhoneNumberConfirmed = false,
                                  TwoFactorEnabled = false,
                                  LockoutEnabled = true,
                                  AccessFailedCount = 0,
                                  FirstName = legacy.FirstName,
                                  LastName = legacy.LastName,
                                  DisplayName = legacy.DisplayName
                              };
        await db.InsertAsync(row, token: cancellationToken);
    }

    private static string ComposeLegacyHash(LegacyUserAuthRow legacy)
    {
        string hash = legacy.PasswordHash ?? string.Empty;
        return legacy.Salt == null ? hash : LegacyHashPrefix + legacy.Salt + "$" + hash;
    }
}

[Table("UserAuth", IsColumnAttributeRequired = false)]
public sealed class LegacyUserAuthRow
{
    public long Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? Salt { get; set; }
    public string? PasswordHash { get; set; }
}

[Table("AspNetUsers", IsColumnAttributeRequired = false)]
public sealed class IdentityUserRow
{
    [PrimaryKey]
    public string Id { get; set; } = null!;

    public string? UserName { get; set; }
    public string? NormalizedUserName { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    public string? ConcurrencyStamp { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
}
