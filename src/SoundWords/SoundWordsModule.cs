using System;
using System.IO.Abstractions;
using Autofac;
using Microsoft.Extensions.Configuration;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.OrmLite;
using SoundWords.Tools;

namespace SoundWords
{
    public class SoundWordsModule : Module
    {
        private readonly IConfiguration _configuration;

        public SoundWordsModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(typeof(SoundWordsModule).Assembly)
                   .Except<SoundWordsConfiguration>(register => register
                                                        .As<ISoundWordsConfiguration>()
                                                        .As<IAppSettings>()
                                                        .SingleInstance())
                   .Except<SoundWordsAppHost>(register => register.As<AppHostBase>().SingleInstance())
                   .Except<BackgroundPool>(register => register.As<IBackgroundPool>().SingleInstance())
                   .AsDefaultInterface()
                   .AsSelf();

            builder.RegisterAssemblyTypes(typeof(SoundWordsModule).Assembly)
                   .AsSelf();

            builder.RegisterType<FileSystem>().As<IFileSystem>();

            builder.RegisterType<MemoryCacheClient>().As<ICacheClient>().SingleInstance();


            //override the default registration validation with your own custom implementation
            builder.RegisterType<CustomRegistrationValidator>().As<IValidator<Register>>();

            builder.Register(context =>
                             {
                                 IOrmLiteDialectProvider dialectProvider = GetDialectProvider(_configuration["DB_TYPE"]);
                                 string defaultConnectionString = _configuration["CONNECTION_STRING"];
                                 OrmLiteConnectionFactory connectionFactory = new OrmLiteConnectionFactory(
                                     defaultConnectionString,
                                     dialectProvider);
                                 connectionFactory.RegisterConnection(
                                     "Users", _configuration["CONNECTION_STRING_USERS"] ?? defaultConnectionString,
                                     dialectProvider);
                                 return connectionFactory;
                             })
                   .As<IDbConnectionFactory>()
                   .SingleInstance();

            builder.Register(c => new CustomOrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>(), "Users"))
                   .As<IUserAuthRepository>()
                   .As<IAuthRepository>()
                   .AsSelf()
                   .SingleInstance();

            builder.Register(c => LogManager.LogFactory).As<ILogFactory>();

            builder.Register(c => _configuration).As<IConfiguration>();

            builder.Register(c => new ServerEventsFeature())
                   .AsSelf()
                   .SingleInstance();

            builder.Register(c =>
                             {
                                 ServerEventsFeature serverEventsFeature = c.Resolve<ServerEventsFeature>();
                                 return new MemoryServerEvents
                                        {
                                            IdleTimeout = serverEventsFeature.IdleTimeout,
                                            HouseKeepingInterval = serverEventsFeature.HouseKeepingInterval,
                                            OnSubscribeAsync = serverEventsFeature.OnSubscribeAsync,
                                            OnUnsubscribeAsync = serverEventsFeature.OnUnsubscribeAsync,
                                            NotifyChannelOfSubscriptions = serverEventsFeature.NotifyChannelOfSubscriptions,
                                            OnError = serverEventsFeature.OnError
                                        };
                             }).As<IServerEvents>()
                   .SingleInstance();
        }

        private static IOrmLiteDialectProvider GetDialectProvider(string dbType)
        {
            switch (dbType)
            {
                case "SQLServer":
                    return SqlServerDialect.Provider;
                case "MySQL":
                    return MySqlConnectorDialect.Provider;
                case "PostgreSQL":
                    return PostgreSqlDialect.Provider;
                default:
                    throw new ArgumentOutOfRangeException(nameof(dbType),
                                                          "The database type is not supported");
            }
        }
    }
}
