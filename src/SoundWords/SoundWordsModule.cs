using System.IO.Abstractions;
using Autofac;
using FluentValidation;
using Microsoft.Extensions.Configuration;
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
