using System;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.MySql;
using FluentMigrator.Runner.Processors.Postgres;
using FluentMigrator.Runner.Processors.SqlServer;
using Microsoft.Extensions.Configuration;
using ServiceStack;
using ServiceStack.Logging;

namespace SoundWords.Tools
{
    public class DbMigrator : IDbMigrator
    {
        private readonly IConfiguration _configuration;
        private readonly ILogFactory _logFactory;

        public DbMigrator(ILogFactory logFactory, IConfiguration configuration)
        {
            _logFactory = logFactory;
            _configuration = configuration;
        }

        public void Migrate()
        {
            IAnnouncer announcer = new ServiceStackLoggingAnnouncer(_logFactory) {ShowSql = true};
            IMigrationProcessorFactory migrationProcessorFactory = GetMigrationProcessorFactory(_configuration["DB_TYPE"]);

            ProcessorOptions options = new ProcessorOptions
                                       {
                                           PreviewOnly = false, // set to true to see the SQL
                                           Timeout = 60
                                       };

            using (IMigrationProcessor processor =
                migrationProcessorFactory.Create(_configuration["CONNECTION_STRING"], announcer,
                                                 options))
            {
                MigrationRunner runner = new MigrationRunner(GetType().Assembly, new RunnerContext(announcer), processor);
                runner.MigrateUp();
            }
        }

        private static IMigrationProcessorFactory GetMigrationProcessorFactory(string dbType)
        {
            switch (dbType)
            {
                case "SQLServer":
                    return new SqlServer2014ProcessorFactory();
                case "MySQL":
                    return new MySqlProcessorFactory();
                case "PostgreSQL":
                    return new PostgresProcessorFactory();
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType),
                                                          "The database type is not supported");
            }
        }
    }
}
