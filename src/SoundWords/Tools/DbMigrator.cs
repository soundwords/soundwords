using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SoundWords.Tools;

public class DbMigrator : IDbMigrator
{
    private static readonly HashSet<string> AppliedRuns = new();
    private static readonly object Gate = new();

    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public DbMigrator(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _loggerFactory = loggerFactory;
        _configuration = configuration;
    }

    public void Migrate()
    {
        string? dbType = _configuration["DB_TYPE"];
        string? mainConnection = _configuration["CONNECTION_STRING"];
        string? usersConnection = _configuration["CONNECTION_STRING_USERS"] ?? mainConnection;

        RunTagged(dbType, mainConnection, "Domain");
        RunTagged(dbType, usersConnection, "Users");
    }

    private void RunTagged(string? dbType, string? connectionString, string tag)
    {
        if (connectionString == null)
        {
            return;
        }

        string runKey = $"{tag}::{connectionString}";
        lock (Gate)
        {
            if (!AppliedRuns.Add(runKey))
            {
                return;
            }
        }

        IServiceProvider serviceProvider = new ServiceCollection()
                                           .AddSingleton(_loggerFactory)
                                           .AddLogging()
                                           .AddFluentMigratorCore()
                                           .ConfigureRunner(rb =>
                                                            {
                                                                ConfigureDialect(rb, dbType);
                                                                rb.WithGlobalConnectionString(connectionString)
                                                                  .ScanIn(typeof(DbMigrator).Assembly)
                                                                  .For.Migrations();
                                                            })
                                           .Configure<RunnerOptions>(o => o.Tags = new[] { tag })
                                           .BuildServiceProvider(false);

        using IServiceScope scope = serviceProvider.CreateScope();
        IMigrationRunner runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }

    private static void ConfigureDialect(IMigrationRunnerBuilder rb, string? dbType)
    {
        switch (dbType)
        {
            case "PostgreSQL":
                rb.AddPostgres();
                break;
            case "MySQL":
                rb.AddMySql8();
                break;
            case "SQLServer":
                rb.AddSqlServer2014();
                break;
            case "SQLite":
                rb.AddSQLite();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(dbType), $"The database type '{dbType}' is not supported");
        }
    }
}
