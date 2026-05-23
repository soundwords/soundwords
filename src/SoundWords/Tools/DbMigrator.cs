using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SoundWords.Tools;

public class DbMigrator : IDbMigrator
{
    private static readonly HashSet<string> AppliedConnectionStrings = new();
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
        string? connectionString = _configuration["CONNECTION_STRING"];

        lock (Gate)
        {
            if (connectionString != null && !AppliedConnectionStrings.Add(connectionString))
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
