using System.Data;
using FluentMigrator;

namespace SoundWords.Migrations;

/// <summary>
/// Copies legacy ServiceStack <c>UserAuth</c> rows into the Identity
/// <c>AspNetUsers</c> table on first migration run.
/// </summary>
/// <remarks>
/// Hash strategy (see <c>Auth/LegacyAwarePasswordHasher.cs</c>):
/// <list type="bullet">
///   <item><description>Salt IS NULL → ServiceStack PBKDF2 hash, wire-compatible with Identity v3. Copied verbatim into PasswordHash.</description></item>
///   <item><description>Salt NOT NULL → ServiceStack SaltedHash (HMAC-SHA-256). Wrapped as <c>SS$&lt;salt&gt;$&lt;hash&gt;</c> so LegacyAwarePasswordHasher can verify it on next login and re-hash to v3 transparently.</description></item>
/// </list>
/// Idempotent: skipped entirely if <c>UserAuth</c> doesn't exist (fresh install), and the SELECT filters out emails already present in <c>AspNetUsers</c>.
/// </remarks>
[Migration(202605231100), FluentMigrator.Tags("Users")]
public class ImportUserAuthMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        if (!Schema.Table("UserAuth").Exists())
        {
            return;
        }

        Execute.WithConnection((connection, transaction) =>
                               {
                                   (string open, string close) = QuoteCharsFor(connection);
                                   string qUserAuth = open + "UserAuth" + close;
                                   string qAspNet = open + "AspNetUsers" + close;

                                   List<UserAuthRow> rows = ReadLegacyUsers(connection, transaction, open, close, qUserAuth);
                                   if (rows.Count == 0)
                                   {
                                       return;
                                   }

                                   HashSet<string> existing = ReadExistingNormalizedEmails(connection, transaction, open, close, qAspNet);

                                   InsertNewUsers(connection, transaction, open, close, qAspNet, rows, existing);
                               });
    }

    private static (string Open, string Close) QuoteCharsFor(IDbConnection connection)
    {
        string typeName = connection.GetType().FullName ?? string.Empty;
        if (typeName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            return ("`", "`");
        }
        if (typeName.Contains("SqlConnection", StringComparison.Ordinal))
        {
            return ("[", "]");
        }
        // PostgreSQL (Npgsql), SQLite, and the ANSI default.
        return ("\"", "\"");
    }

    private static List<UserAuthRow> ReadLegacyUsers(IDbConnection connection, IDbTransaction transaction,
                                                    string open, string close, string qUserAuth)
    {
        List<UserAuthRow> rows = new();

        using IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"SELECT {open}UserName{close}, {open}Email{close}, {open}FirstName{close}, " +
            $"{open}LastName{close}, {open}DisplayName{close}, {open}Salt{close}, {open}PasswordHash{close} " +
            $"FROM {qUserAuth} WHERE {open}Email{close} IS NOT NULL";

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            string? email = reader["Email"] as string;
            if (string.IsNullOrEmpty(email))
            {
                continue;
            }

            rows.Add(new UserAuthRow(
                         UserName: reader["UserName"] as string,
                         Email: email,
                         FirstName: reader["FirstName"] as string,
                         LastName: reader["LastName"] as string,
                         DisplayName: reader["DisplayName"] as string,
                         Salt: reader["Salt"] as string,
                         PasswordHash: reader["PasswordHash"] as string));
        }

        return rows;
    }

    private static HashSet<string> ReadExistingNormalizedEmails(IDbConnection connection, IDbTransaction transaction,
                                                                string open, string close, string qAspNet)
    {
        HashSet<string> existing = new(StringComparer.Ordinal);

        using IDbCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {open}NormalizedEmail{close} FROM {qAspNet} WHERE {open}NormalizedEmail{close} IS NOT NULL";

        using IDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.GetValue(0) is string normalized)
            {
                existing.Add(normalized);
            }
        }

        return existing;
    }

    private static void InsertNewUsers(IDbConnection connection, IDbTransaction transaction,
                                       string open, string close, string qAspNet,
                                       List<UserAuthRow> rows, HashSet<string> existing)
    {
        string columns = string.Join(", ", new[]
                                           {
                                               "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail",
                                               "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
                                               "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
                                               "AccessFailedCount", "FirstName", "LastName", "DisplayName"
                                           }.Select(c => open + c + close));

        const string paramList =
            "@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, " +
            "@EmailConfirmed, @PasswordHash, @SecurityStamp, @ConcurrencyStamp, " +
            "@PhoneNumberConfirmed, @TwoFactorEnabled, @LockoutEnabled, " +
            "@AccessFailedCount, @FirstName, @LastName, @DisplayName";

        string sql = $"INSERT INTO {qAspNet} ({columns}) VALUES ({paramList})";

        foreach (UserAuthRow row in rows)
        {
            string handle = string.IsNullOrEmpty(row.UserName) ? row.Email : row.UserName!;
            string normalizedEmail = row.Email.ToUpperInvariant();
            if (existing.Contains(normalizedEmail))
            {
                continue;
            }

            string passwordHash;
            if (row.PasswordHash == null)
            {
                continue;
            }
            if (row.Salt == null)
            {
                passwordHash = row.PasswordHash;
            }
            else
            {
                passwordHash = "SS$" + row.Salt + "$" + row.PasswordHash;
            }

            using IDbCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            AddParameter(command, "@Id", Guid.NewGuid().ToString("N"));
            AddParameter(command, "@UserName", handle.ToLowerInvariant());
            AddParameter(command, "@NormalizedUserName", handle.ToUpperInvariant());
            AddParameter(command, "@Email", row.Email);
            AddParameter(command, "@NormalizedEmail", normalizedEmail);
            AddParameter(command, "@EmailConfirmed", true);
            AddParameter(command, "@PasswordHash", passwordHash);
            AddParameter(command, "@SecurityStamp", Guid.NewGuid().ToString("N"));
            AddParameter(command, "@ConcurrencyStamp", Guid.NewGuid().ToString("N"));
            AddParameter(command, "@PhoneNumberConfirmed", false);
            AddParameter(command, "@TwoFactorEnabled", false);
            AddParameter(command, "@LockoutEnabled", true);
            AddParameter(command, "@AccessFailedCount", 0);
            AddParameter(command, "@FirstName", (object?)row.FirstName ?? DBNull.Value);
            AddParameter(command, "@LastName", (object?)row.LastName ?? DBNull.Value);
            AddParameter(command, "@DisplayName", (object?)row.DisplayName ?? DBNull.Value);
            command.ExecuteNonQuery();

            existing.Add(normalizedEmail);
        }
    }

    private static void AddParameter(IDbCommand command, string name, object value)
    {
        IDbDataParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record UserAuthRow(
        string? UserName,
        string Email,
        string? FirstName,
        string? LastName,
        string? DisplayName,
        string? Salt,
        string? PasswordHash);
}
