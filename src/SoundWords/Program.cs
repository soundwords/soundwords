using Autofac;
using Autofac.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using LinqToDB;
using LinqToDB.AspNet;
using LinqToDB.AspNet.Logging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using SoundWords.Auth;
using SoundWords.Data;
using SoundWords.Hubs;
using SoundWords.Tools;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, loggerConfiguration) =>
                        {
                            loggerConfiguration.MinimumLevel.Debug()
                                               .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                               .Enrich.FromLogContext()
                                               .WriteTo.Console(
                                                   outputTemplate:
                                                   "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {SourceContext} {Message}{NewLine}{Exception}");
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
    scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseForwardedHeaders(new ForwardedHeadersOptions
                            {
                                ForwardedHeaders =
                                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                            });
}

app.UseSerilogRequestLogging();
app.UseStaticFiles();

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
