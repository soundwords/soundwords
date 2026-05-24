using System.IO.Abstractions;
using Amazon.Runtime;
using Amazon.S3;
using Autofac;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using SoundWords.Auth;
using SoundWords.Media;
using SoundWords.Models;
using SoundWords.Social;
using SoundWords.Tools;
using TagFile = TagLib.File;

namespace SoundWords;

public class SoundWordsModule : Module
{
    private readonly IConfiguration _configuration;

    public SoundWordsModule(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<SoundWordsConfiguration>().As<ISoundWordsConfiguration>().SingleInstance();
        builder.RegisterType<BackgroundPool>().As<IBackgroundPool>().SingleInstance();
        builder.RegisterType<DbMigrator>().As<IDbMigrator>().SingleInstance();
        builder.RegisterType<MarkdownTool>().As<IMarkdownTool>().SingleInstance();
        builder.RegisterType<MetaDataTool>().As<IMetaDataTool>().SingleInstance();
        builder.RegisterType<RecordingRepository>().As<IRecordingRepository>().InstancePerLifetimeScope();
        builder.RegisterType<RebuildJob>().AsSelf().InstancePerDependency();
        builder.RegisterType<LegacyUserSync>().As<ILegacyUserSync>().InstancePerLifetimeScope();

        builder.Register(context =>
                         {
                             ISoundWordsConfiguration config = context.Resolve<ISoundWordsConfiguration>();
                             AmazonS3Config s3Config = new()
                                                       {
                                                           ServiceURL = config.S3Endpoint
                                                                        ?? throw new InvalidOperationException(
                                                                            "S3_ENDPOINT not configured."),
                                                           ForcePathStyle = true     // MinIO path-style: /{bucket}/{key}
                                                       };
                             BasicAWSCredentials creds = new(
                                 config.S3AccessKey ?? throw new InvalidOperationException("S3_ACCESS_KEY not configured."),
                                 config.S3SecretKey ?? throw new InvalidOperationException("S3_SECRET_KEY not configured."));
                             return new AmazonS3Client(creds, s3Config);
                         })
               .As<IAmazonS3>()
               .SingleInstance();
        builder.RegisterType<S3SignedMediaUrls>().As<ISignedMediaUrls>().SingleInstance();

        builder.RegisterType<FileSystem>().As<IFileSystem>().SingleInstance();

        builder.RegisterAssemblyTypes(typeof(SoundWordsModule).Assembly)
               .Where(t => t.IsAssignableTo<IValidator>() && !t.IsAbstract)
               .AsImplementedInterfaces();

        builder.Register<Func<string, bool, TagFile.IFileAbstraction>>(c =>
                                                                       {
                                                                           IFileSystem fs = c.Resolve<IFileSystem>();
                                                                           return (path, writable) => new FileAbstraction(fs, path, writable);
                                                                       });

        builder.Register(_ => _configuration).As<IConfiguration>();
    }
}
