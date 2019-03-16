using System;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.Logging.Serilog;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;
using SoundWords.Models;
using SoundWords.Services;
using SoundWords.Tools;

namespace SoundWords
{
    public class SoundWordsAppHost : AppHostBase
    {
        private readonly AutofacIocAdapter _autofacIocAdapter;
        private ILog _logger;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;
        private readonly ServerEventsFeature _serverEventsFeature;
        public static ISoundWordsConfiguration Configuration { get; private set; }

        public SoundWordsAppHost(AutofacIocAdapter autofacIocAdapter, IHostingEnvironment hostingEnvironment, IConfiguration configuration, ServerEventsFeature serverEventsFeature) : base("SoundWords", typeof (SoundWordsAppHost).Assembly)
        {
            _autofacIocAdapter = autofacIocAdapter;
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
            _serverEventsFeature = serverEventsFeature;
        }

        public override void Bind(IApplicationBuilder app)
        {
            base.Bind(app);
            LogManager.LogFactory = new SerilogFactory();
            _logger = LogManager.GetLogger(GetType());
        }

        public override void Configure(Container container)
        {
            _logger.Debug("Configuring");
            
            container.Adapter = _autofacIocAdapter;
            AppSettings = container.Resolve<IAppSettings>();

            //Set JSON web services to return idiomatic JSON camelCase properties            
            JsConfig.EmitCamelCaseNames = true;

            GlobalHtmlErrorHttpHandler = new RazorHandler("/oops");

            Configuration = container.Resolve<ISoundWordsConfiguration>();

            SetConfig(new HostConfig
            {
                DebugMode = _hostingEnvironment.IsDevelopment() || Configuration.DebugMode,
                WebHostUrl = _configuration["SITE_URL"] 
            });
 
            ConfigureAuth(container);

            MimeTypes.ExtensionMimeTypes["manifest"] = "text/cache-manifest";
            MimeTypes.ExtensionMimeTypes["appcache"] = "text/cache-manifest";
            MimeTypes.ExtensionMimeTypes["ico"] = "image/x-icon";
            Config.AllowFileExtensions.Add("manifest");
            Config.AllowFileExtensions.Add("appcache");

            Plugins.Add(new RazorFormat());
            Plugins.Add(new ValidationFeature());
            Plugins.Add(_serverEventsFeature);

            IDbMigrator migrator = container.Resolve<IDbMigrator>();
            migrator.Migrate();

            OrmLiteConfig.InsertFilter += (command, o) =>
            {
                DbEntity entity = o as DbEntity;
                if (entity == null) return;

                entity.CreatedOn = DateTime.UtcNow;
                entity.ModifiedOn = DateTime.UtcNow;

            };

            OrmLiteConfig.UpdateFilter += (command, o) =>
            {
                DbEntity entity = o as DbEntity;
                if (entity == null) return;

                entity.ModifiedOn = DateTime.UtcNow;
            };
        }

        private void ConfigureAuth(Container container)
        {
            //Enable and register existing services you want this host to make use of.
            //Look in Web.config for examples on how to configure your oauth providers, e.g. oauth.facebook.AppId, etc.
            
            //Register all Authentication methods you want to enable for this web app.            
            Plugins.Add(new AuthFeature(
                            () => new CustomUserSession(), //Use your own typed Custom UserSession type
                            new IAuthProvider[]
                                {
                                    new CredentialsAuthProvider() //HTML Form post of UserName/Password credentials
                                }) { HtmlRedirect = "/Login"});

            //Provide service for new users to register so they can login with supplied credentials.
            Plugins.Add(new RegistrationFeature());
           
            //Use OrmLite DB Connection to persist the UserAuth and AuthProvider info
            var authRepo = container.Resolve<CustomOrmLiteAuthRepository>();
            //If using and RDBMS to persist UserAuth, we must create required tables
            if (AppSettings.Get("RECREATE_AUTH_TABLES", false))
            {
                _logger.Info("Recreating auth tables");
                authRepo.DropAndReCreateTables(); //Drop and re-create all Auth and registration tables
            }
            else
            {
                _logger.Info("Creating missing auth tables");
                authRepo.InitSchema(); //Create only the missing tables
            }

            Plugins.Add(new RequestLogsFeature());
        }

        public override void OnAfterInit()
        {
            base.OnAfterInit();

            RegisterAs<CustomRegistrationValidator, IValidator<Register>>();            
        }

        public override string ResolveAbsoluteUrl(string virtualPath, IRequest httpReq)
        {
            string protocolFull = $"{Configuration.Protocol}://";
            return base.ResolveAbsoluteUrl(virtualPath, httpReq).Replace("http://", protocolFull);
        }
    }
}