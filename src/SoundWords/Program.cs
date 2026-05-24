using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using LinqToDB;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Serilog;
using Serilog.Events;
using SoundWords.Auth;
using SoundWords.Data;
using SoundWords.Hubs;
using SoundWords.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Surface Serilog's own diagnostics on stderr so misconfigured sinks
// (bad SEQ_URL, 401, network errors) don't fail silently.
Serilog.Debugging.SelfLog.Enable(Console.Error);

builder.Host.UseSerilog((context, loggerConfiguration) =>
                        {
                            loggerConfiguration.MinimumLevel.Debug()
                                               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                               .Enrich.FromLogContext()
                                               .Enrich.WithProperty("Application", "SoundWords")
                                               .WriteTo.Console(
                                                   outputTemplate:
                                                   "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {SourceContext} {Message}{NewLine}{Exception}");

                            string? seqUrl = context.Configuration["SEQ_URL"];
                            if (!string.IsNullOrEmpty(seqUrl))
                            {
                                string? seqApiKey = context.Configuration["SEQ_API_KEY"];
                                loggerConfiguration.WriteTo.Seq(seqUrl, apiKey: seqApiKey);
                                Console.WriteLine($"[Serilog] Seq sink enabled → {seqUrl}");
                            }
                            else
                            {
                                Console.WriteLine("[Serilog] SEQ_URL not set; Seq sink disabled.");
                            }
                        });

string dbType = builder.Configuration["DB_TYPE"]
                ?? throw new InvalidOperationException("DB_TYPE not configured");
string mainConnectionString = builder.Configuration["CONNECTION_STRING"]
                              ?? throw new InvalidOperationException("CONNECTION_STRING not configured");
string usersConnectionString = builder.Configuration["CONNECTION_STRING_USERS"] ?? mainConnectionString;

builder.Services.AddLinqToDBContext<SoundWordsDb>((provider, options) =>
                                                     options.UseConnectionString(
                                                         SoundWordsDb.GetProvider(dbType),
                                                         mainConnectionString)
                                                            .UseDefaultLogging(provider),
                                                 ServiceLifetime.Transient);

builder.Services.AddDbContext<AuthDbContext>(options =>
                                             {
                                                 switch (dbType)
                                                 {
                                                     case "PostgreSQL":
                                                         options.UseNpgsql(usersConnectionString);
                                                         break;
                                                     case "MySQL":
                                                         options.UseMySQL(usersConnectionString);
                                                         break;
                                                     case "SQLServer":
                                                         options.UseSqlServer(usersConnectionString);
                                                         break;
                                                     case "SQLite":
                                                         options.UseSqlite(usersConnectionString);
                                                         break;
                                                     default:
                                                         throw new InvalidOperationException(
                                                             $"DB_TYPE '{dbType}' is not supported. Use PostgreSQL, MySQL, SQLServer, or SQLite.");
                                                 }
                                             });

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                                                            {
                                                                options.User.RequireUniqueEmail = true;
                                                                options.Password.RequiredLength = 8;
                                                            })
       .AddEntityFrameworkStores<AuthDbContext>()
       .AddDefaultTokenProviders();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, LegacyAwarePasswordHasher>();

builder.Services.ConfigureApplicationCookie(o =>
                                            {
                                                o.LoginPath = "/Account/Login";
                                                o.LogoutPath = "/Account/Logout";
                                                o.AccessDeniedPath = "/Account/Login";
                                            });

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

builder.Services.AddMvc();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
                                                  {
                                                      containerBuilder.RegisterModule(
                                                          new SoundWordsModule(context.Configuration));
                                                  });

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IDbMigrator>().Migrate();
    await scope.ServiceProvider.GetRequiredService<ILegacyUserSync>().SyncAsync();
}

// Trust X-Forwarded-* headers BEFORE any other middleware so Request.Scheme,
// Request.Host, and connection IP reflect the original client request. Behind
// Cloudflare + Traefik these arrive from non-loopback addresses, so the
// default KnownNetworks/KnownProxies allowlist (loopback only) drops them —
// clear it and rely on the edge proxies to strip client-supplied values.
ForwardedHeadersOptions forwardedHeaders = new()
                                           {
                                               ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                                                  | ForwardedHeaders.XForwardedProto
                                                                  | ForwardedHeaders.XForwardedHost
                                           };
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseSerilogRequestLogging();
app.UseStaticFiles();

// Mount {CUSTOM_FOLDER}/content at /content — mirrors the FileSystemMapping
// the old AppHost used so site-specific images, logos, RSS artwork, etc.
// can live outside the deployed wwwroot.
ISoundWordsConfiguration soundWordsConfiguration =
    app.Services.GetRequiredService<ISoundWordsConfiguration>();
string contentRoot = Path.Combine(soundWordsConfiguration.CustomFolder, "content");
if (Directory.Exists(contentRoot))
{
    app.UseStaticFiles(new StaticFileOptions
                       {
                           FileProvider = new PhysicalFileProvider(contentRoot),
                           RequestPath = "/content"
                       });
}

string? pathBase = builder.Configuration["PATH_BASE"];
if (pathBase != null)
{
    app.UsePathBase(pathBase);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapHub<RebuildHub>("/hubs/rebuild");

app.Run();
