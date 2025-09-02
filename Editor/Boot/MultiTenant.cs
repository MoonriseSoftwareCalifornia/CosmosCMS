// <copyright file="MultiTenant.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Editor.Boot
{
    using System;
    using System.Configuration;
    using System.Text.RegularExpressions;
    using System.Threading.RateLimiting;
    using System.Threading.Tasks;
    using System.Web;
    using AspNetCore.Identity.FlexDb;
    using AspNetCore.Identity.FlexDb.Extensions;
    using Azure.Identity;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Hubs;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Services.Configurations;
    using Cosmos.DynamicConfig;
    using Cosmos.Editor.Data.Logic;
    using Cosmos.Editor.Services;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///  Creates a multi-tenant web application.
    /// </summary>
    internal class MultiTenant
    {
        /// <summary>
        /// Builds a multi-tenant web application.
        /// </summary>
        /// <param name="builder">Web application builder.</param>
        /// <returns>Returns a web application ready to run.</returns>
        internal static WebApplication BuildApp(WebApplicationBuilder builder)
        {
            // Add memory cache for Cosmos data logic and other services.
            builder.Services.AddMemoryCache();

            // Create one instance of the DefaultAzureCredential to be used throughout the application.
            var defaultAzureCredential = new DefaultAzureCredential();
            builder.Services.AddSingleton(defaultAzureCredential);

            // The following line enables Application Insights telemetry collection.
            // See: https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcore6
            builder.Services.AddApplicationInsightsTelemetry();

            // Add Cosmos Options
            // Get the boot variables loaded, and
            // do some validation to make sure Cosmos can boot up
            // based on the values given.
            var cosmosStartup = new CosmosStartup(builder.Configuration);
            var option = cosmosStartup.Build();
            builder.Services.AddSingleton(option);

            // The next two services have to appear before DB Context.
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IDynamicConfigurationProvider, DynamicConfigurationProvider>();

            builder.Services.AddSingleton<MultiDatabaseManagementUtilities>();

            // Note that this is transient, meaning for each request this is regenerated.
            // Multi-tenant support is enabled because each request may have a different domain name and connection
            // string information.
            builder.Services.AddTransient<ApplicationDbContext>(serviceProvider =>
            {
                var optionsBuilder = GetDynamicOptionsBuilder(serviceProvider);
                return new ApplicationDbContext(optionsBuilder.Options);
            });

            // This service has to appear right after DB Context.
            builder.Services.AddTransient<IEditorSettings, EditorSettings>();

            // Add Cosmos Identity here
            builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
                  options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultUI() // Use this if Identity Scaffolding added
                .AddDefaultTokenProviders();

            // Add shared data protection here
            builder.Services.AddCosmosCmsDataProtection(builder.Configuration, defaultAzureCredential);

            // ===========================================================
            // SUPPORTED OAuth Providers
            //-------------------------------
            // Add Google if keys are present
            var googleOAuth = builder.Configuration.GetSection("GoogleOAuth").Get<OAuth>();

            if (googleOAuth != null && googleOAuth.IsConfigured())
            {
                builder.Services.AddAuthentication().AddGoogle(options =>
                {
                    options.ClientId = googleOAuth.ClientId;
                    options.ClientSecret = googleOAuth.ClientSecret;
                });
            }

            // ---------------------------------
            // Add Microsoft if keys are present
            var entraIdOAuth = builder.Configuration.GetSection("MicrosoftOAuth").Get<OAuth>();

            if (entraIdOAuth != null && entraIdOAuth.IsConfigured())
            {
                builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
                {
                    options.ClientId = entraIdOAuth.ClientId;
                    options.ClientSecret = entraIdOAuth.ClientSecret;

                    if (!string.IsNullOrEmpty(entraIdOAuth.TenantId))
                    {
                        // This is for registered apps in the Azure portal that are single tenant.
                        options.AuthorizationEndpoint = $"https://login.microsoftonline.com/{entraIdOAuth.TenantId}/oauth2/v2.0/authorize";
                        options.TokenEndpoint = $"https://login.microsoftonline.com/{entraIdOAuth.TenantId}/oauth2/v2.0/token";
                    }

                    if (!string.IsNullOrEmpty(entraIdOAuth.CallbackDomain))
                    {
                        options.Events.OnRedirectToAuthorizationEndpoint = context =>
                        {
                            var redirectUrl = Regex.Replace(context.RedirectUri, "redirect_uri=(.)+%2Fsignin-", $"redirect_uri=https%3A%2F%2F{entraIdOAuth.CallbackDomain}%2Fsignin-");
                            context.Response.Redirect(redirectUrl);
                            return Task.CompletedTask;
                        };
                    }
                });
            }

            // Add Azure CDN/Front door configuration here.
            builder.Services.Configure<CdnService>(builder.Configuration.GetSection("AzureCdnConfig"));

            // Add Email services
            builder.Services.AddCosmosEmailServices(builder.Configuration);

            // Add the BLOB and File Storage contexts for Cosmos
            builder.Services.AddCosmosStorageContext(builder.Configuration);

            // Cosmos CMS Editor data logic layer.
            builder.Services.AddTransient<ArticleEditLogic>();

            // This is used by the ViewRenderingService 
            // to export web pages for external editing.
            builder.Services.AddScoped<IViewRenderService, ViewRenderService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    "AllCors",
                    policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod();
                    });
            });

            // Add this before identity
            // See also: https://learn.microsoft.com/en-us/aspnet/core/performance/caching/response?view=aspnetcore-7.0
            builder.Services.AddControllersWithViews();

            builder.Services.AddRazorPages();

            builder.Services.AddMvc()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver())
                .AddRazorPagesOptions(options =>
                {
                    // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full 
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });

            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
            builder.Services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = "CosmosAuthCookie";
                options.ExpireTimeSpan = TimeSpan.FromDays(5);
                options.SlidingExpiration = true;

                // This section docs are here: https://docs.microsoft.com/en-us/aspnet/core/security/authentication/scaffold-identity?view=aspnetcore-3.1&tabs=visual-studio#full
                // The following is when using Docker container with a proxy like
                // Azure front door. It ensures relative paths for redirects
                // which is necessary when the public DNS at Front door is www.mycompany.com 
                // and the DNS of the App Service is something like myappservice.azurewebsites.net.
                options.Events.OnRedirectToLogin = x =>
                {
                    var queryParams = System.Web.HttpUtility.ParseQueryString(x.Request.QueryString.Value);

                    var website = queryParams["ccmswebsite"];
                    var opt = queryParams["ccmsopt"];
                    var email = queryParams["ccmsemail"];
                    queryParams.Remove("ccmswebsite");
                    queryParams.Remove("ccmsopt");
                    queryParams.Remove("ccmsemail");
                    var queryString = HttpUtility.UrlEncode(queryParams.ToString());

                    if (x.Request.Path.Equals("/Preview", StringComparison.InvariantCultureIgnoreCase))
                    {
                        x.Response.Redirect($"/Identity/Account/Login?returnUrl=/Home/Preview?{queryString}");
                    }

                    x.Response.Redirect($"/Identity/Account/Login?returnUrl={x.Request.Path}&{queryString}");
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToLogout = x =>
                {
                    x.Response.Redirect("/Identity/Account/Logout");
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = x =>
                {
                    x.Response.Redirect("/Identity/Account/AccessDenied");
                    return Task.CompletedTask;
                };
            });

            // BEGIN
            // When deploying to a Docker container, the OAuth redirect_url
            // parameter may have http instead of https.
            // Providers often do not allow http because it is not secure.
            // So authentication will fail.
            // Article below shows instructions for fixing this.
            //
            // NOTE: There is a companion secton below in the Configure method. Must have this
            //
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;

                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            // END
            // builder.Services.AddResponseCaching();

            // Add the SignalR service.
            // If there is a DB connection, then use SQL backplane.
            // See: https://github.com/IntelliTect/IntelliTect.AspNetCore.SignalR.SqlServer
            builder.Services.AddSignalR();

            // Throttle certain endpoints to protect the website.
            builder.Services.AddRateLimiter(_ => _
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 4;
                    options.Window = TimeSpan.FromSeconds(8);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 2;
                }));

            // This service is used to run startup tasks asynchronously.
            builder.Services.AddScoped<IStartupTaskService, StartupTaskService>();

            // Build the application.
            var app = builder.Build();

            // Run the startup tasks asynchronously.
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var startupTask = services.GetRequiredService<IStartupTaskService>();
                var t = startupTask.RunAsync();
                t.Wait();
            }

            // Enable data protection services for Cosmos CMS.
            app.UseCosmosCmsDataProtection();

            // Domain middleware used to get the domain name of the current request.
            app.UseMiddleware<DomainMiddleware>();

            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection(); // See: https://github.com/dotnet/aspnetcore/issues/18594
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.MapGet("ccms__antiforgery/token", (IAntiforgery forgeryService, HttpContext context) =>
            {
                var tokens = forgeryService.GetAndStoreTokens(context);
                context.Response.Headers["XSRF-TOKEN"] = tokens.RequestToken;
                return Results.Ok();
            });

            // Point to the route that will return the SignalR Hub.
            app.MapHub<LiveEditorHub>("/___cwps_hubs_live_editor");

            app.MapControllerRoute(
                "MsValidation",
                ".well-known/microsoft-identity-association.json",
                new { controller = "Home", action = "GetMicrosoftIdentityAssociation" }).AllowAnonymous();

            app.MapControllerRoute(
                "MyArea",
                "{area:exists}/{controller=Home}/{action=Index}/{id?}");

            app.MapControllerRoute(
                name: "pub",
                pattern: "pub/{*index}",
                defaults: new { controller = "Pub", action = "Index" });

            app.MapControllerRoute(
                "storydesk",
                pattern: "storydesk/index/{id?}",
                defaults: new { controller = "Cosmos___StoryDesk", action = "Index" });

            app.MapControllerRoute(
                    "default",
                    "{controller=Home}/{action=Index}/{id?}");

            // Deep path
            app.MapFallbackToController("Index", "Home");

            app.MapRazorPages();

            return app;
        }

        /// <summary>
        /// Gets the DbContext options using the dynamic configuration provider.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <returns>DbApplicationContext.</returns>
        private static DbContextOptionsBuilder<ApplicationDbContext> GetDynamicOptionsBuilder(IServiceProvider services)
        {
            var connectionStringProvider = services.GetRequiredService<IDynamicConfigurationProvider>();
            var connectionString = connectionStringProvider.GetDatabaseConnectionString();

            // Note: This may be null if the cookie or website URL has not yet been set.
            if (string.IsNullOrEmpty(connectionString))
            {
                return new DbContextOptionsBuilder<ApplicationDbContext>();
            }

            return CosmosDbOptionsBuilder.GetDbOptionsBuilder<ApplicationDbContext>(connectionString);
        }
    }
}
