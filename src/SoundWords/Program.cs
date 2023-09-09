using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using ServiceStack;

Log.Logger = new LoggerConfiguration()
             .WriteTo.Console()
             .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    LicenseUtils.RegisterLicense(builder.Configuration["servicestack:license"]);

    builder.Host.UseSerilog((context, loggerConfiguration) =>
                            {
                                loggerConfiguration.MinimumLevel.Debug()
                                                   .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                                   .Enrich.FromLogContext()
                                                   .WriteTo.Console(
                                                       outputTemplate:
                                                       "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {SourceContext} {Message}{NewLine}{Exception}");
                            });
#if DEBUG
    builder.Services.AddMvc(options => options.EnableEndpointRouting = false).AddRazorRuntimeCompilation();
#else
    builder.Services.AddMvc(options => options.EnableEndpointRouting = false);
#endif

    builder.Services.Configure<CookiePolicyOptions>(options =>
                                                    {
                                                        // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                                                        options.CheckConsentNeeded = context => true;
                                                        options.MinimumSameSitePolicy = SameSiteMode.None;
                                                    });

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
           .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);
    // LogManager.LogFactory = new SerilogFactory();

    builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
    builder.Host.ConfigureContainer<ContainerBuilder>((context, containerBuilder) =>
                                                      {
                                                          containerBuilder.RegisterModule(new SoundWordsModule(context.Configuration));
                                                      });

    WebApplication app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseForwardedHeaders(new ForwardedHeadersOptions
                                {
                                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                                });
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        // app.UseHsts();
        // app.UseHttpsRedirection();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseStaticFiles();
    app.UseCookiePolicy();
    app.UseAuthentication();

    string pathBase = builder.Configuration["PATH_BASE"];
    if (pathBase != null)
    {
        app.UsePathBase(pathBase);
    }

    app.UseServiceStack(app.Services.GetService<AppHostBase>());

    app.UseMvc(routes =>
               {
                   routes.MapRoute(
                       "default",
                       "{controller=Home}/{action=Index}/{id?}");
               });

    Log.Information("Starting web host");
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
