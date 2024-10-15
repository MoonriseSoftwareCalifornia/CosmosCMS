// <copyright file="Startup.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.RateLimiting;
    using System.Threading.Tasks;
    using System.Web;
    using AspNetCore.Identity.CosmosDb.Extensions;
    using Azure.Identity;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Hubs;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Common.Services.Configurations;
    using Cosmos.Common.Services.PowerBI;
    using Cosmos.Editor.Services;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
    using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Diagnostics;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///     Startup class for the website.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">Website <see cref="IConfiguration"/>.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///     Gets configuration for the website.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     Method configures services for the website.
        /// </summary>
        /// <param name="services">Website <see cref="IServiceCollection"/>.</param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Get the boot variables loaded, and
            // do some validation to make sure Cosmos can boot up
            // based on the values given.
            var cosmosStartup = new CosmosStartup(Configuration);

            // Add the memory cache for the website.
            services.AddMemoryCache();

            // Create one instance of the DefaultAzureCredential to be used throughout the application.
            var defaultAzureCredential = new DefaultAzureCredential();
            services.AddSingleton(defaultAzureCredential);

            // Add Cosmos Options
            var option = cosmosStartup.Build();
            services.AddSingleton(option);

            // The following line enables Application Insights telemetry collection.
            // See: https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcore6
            services.AddApplicationInsightsTelemetry();

            // The Cosmos connection string
            var connectionString = Configuration.GetConnectionString("ApplicationDbContextConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                // It has been the case in the past where Linux web apps want upper case variable names.
                // This is a check for that.
                var list = Configuration.GetSection("ConnectionStrings").GetChildren();
                var keys = new List<string>();
                foreach (var item in list)
                {
                    keys.Add(item.Key);
                }

                throw new ArgumentException($"STARTUP: ApplicationDbContextConnection is null or empty. Did find: {string.Join(';', keys)}");
            }

            // Name of the Cosmos database to use
            var cosmosIdentityDbName = Configuration.GetValue<string>("CosmosIdentityDbName");
            if (string.IsNullOrEmpty(cosmosIdentityDbName))
            {
                cosmosIdentityDbName = "cosmoscms";
            }

            // If this is set, the Cosmos identity provider will:
            // 1. Create the database if it does not already exist.
            // 2. Create the required containers if they do not already exist.
            // IMPORTANT: Remove this variable if after first run. It will improve startup performance.

            // If the following is set, it will create the Cosmos database and
            //  required containers.
            if (option.Value.SiteSettings.AllowSetup)
            {
                var tempParts = connectionString.Split(";").Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);
                var tempEndPoint = tempParts["AccountEndpoint"];
                var tempBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

                if (tempParts["AccountKey"] == "AccessToken")
                {
                    tempBuilder.UseCosmos(tempEndPoint, defaultAzureCredential, cosmosIdentityDbName);
                }
                else
                {
                    tempBuilder.UseCosmos(connectionString, cosmosIdentityDbName);
                }

                using var dbContext = new ApplicationDbContext(tempBuilder.Options);
                dbContext.Database.EnsureCreated();
            }

            // Add the Cosmos database context here
            var cosmosRegionName = Configuration.GetValue<string>("CosmosRegionName");
            var conpartsDict = connectionString.Split(";").Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);
            var endpoint = conpartsDict["AccountEndpoint"];
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                if (string.IsNullOrEmpty(cosmosRegionName))
                {
                    if (conpartsDict["AccountKey"] == "AccessToken")
                    {
                        options.UseCosmos(endpoint, defaultAzureCredential, cosmosIdentityDbName);
                    }
                    else
                    {
                        options.UseCosmos(connectionString, cosmosIdentityDbName);
                    }
                }
                else
                {
                    if (conpartsDict["AccountKey"] == "AccessToken")
                    {
                        options.UseCosmos(endpoint, defaultAzureCredential, cosmosIdentityDbName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                    }
                    else
                    {
                        options.UseCosmos(connectionString, cosmosIdentityDbName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                    }
                }
            });

            // Add Cosmos Identity here
            services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
                  options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultUI() // Use this if Identity Scaffolding added
                .AddDefaultTokenProviders();

            // Add shared data protection here
            var container = BlobService.ServiceCollectionExtensions.GetBlobContainerClient(Configuration, defaultAzureCredential, "ekyes");
            container.CreateIfNotExists();
            services.AddDataProtection().UseCryptographicAlgorithms(
                new AuthenticatedEncryptorConfiguration
                {
                    EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                    ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
                }).PersistKeysToAzureBlobStorage(container.GetBlobClient("keys.xml"));

            // ===========================================================
            // SUPPORTED OAuth Providers

            //-------------------------------
            // Add Google if keys are present
            var googleOAuth = Configuration.GetSection("GoogleOAuth").Get<OAuth>();

            if (googleOAuth != null && googleOAuth.IsConfigured())
            {
                services.AddAuthentication().AddGoogle(options =>
                {
                    options.ClientId = googleOAuth.ClientId;
                    options.ClientSecret = googleOAuth.ClientSecret;
                });
            }

            // ---------------------------------
            // Add Microsoft if keys are present
            var entraIdOAuth = Configuration.GetSection("MicrosoftOAuth").Get<OAuth>();

            if (entraIdOAuth != null && entraIdOAuth.IsConfigured())
            {
                services.AddAuthentication().AddMicrosoftAccount(options =>
                {
                    options.ClientId = entraIdOAuth.ClientId;
                    options.ClientSecret = entraIdOAuth.ClientSecret;
                });
            }

            // Add Power BI Token Service.
            services.AddScoped(typeof(PowerBiTokenService));
            services.Configure<PowerBiAuth>(Configuration.GetSection("PowerBiAuth"));

            // Add Azure CDN/Frontdoor configuration here.
            services.Configure<CdnService>(Configuration.GetSection("AzureCdnConfig"));

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(3600);
                options.Cookie.IsEssential = true;
            });

            // Add Email services
            services.AddCosmosEmailServices(Configuration);

            // Add the BLOB and File Storage contexts for Cosmos
            services.AddCosmosStorageContext(Configuration);

            services.AddTransient<ArticleEditLogic>();

            // This is used by the ViewRenderingService 
            // to export web pages for external editing.
            services.AddScoped<IViewRenderService, ViewRenderService>();

            services.AddCors(options =>
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
            services.AddControllersWithViews();

            services.AddRazorPages();

            services.AddMvc()
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
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);
            });

            services.ConfigureApplicationCookie(options =>
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
                    var queryString = HttpUtility.UrlEncode(x.Request.QueryString.Value);
                    if (x.Request.Path.Equals("/Preview", StringComparison.InvariantCultureIgnoreCase))
                    {
                        x.Response.Redirect($"/Identity/Account/Login?returnUrl=/Home/Preview{queryString}");
                    }

                    x.Response.Redirect($"/Identity/Account/Login?returnUrl={x.Request.Path}{queryString}");
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
            services.Configure<ForwardedHeadersOptions>(options =>
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
            services.AddResponseCaching();

            // Add the SignalR service.
            // If there is a DB connection, then use SQL backplane.
            // See: https://github.com/IntelliTect/IntelliTect.AspNetCore.SignalR.SqlServer
            var signalRConnection = Configuration.GetConnectionString("CosmosSignalRConnection");
            if (string.IsNullOrEmpty(signalRConnection))
            {
                services.AddSignalR();
            }
            else
            {
                var appUli = new Uri(option.Value.SiteSettings.PublisherUrl);
                var sendpoint = new Microsoft.Azure.SignalR.ServiceEndpoint(signalRConnection);
                var appName = GetHashString(appUli.DnsSafeHost);

                services.AddSignalR().AddAzureSignalR(config =>
                {
                    config.Endpoints = new[] { sendpoint };
                    config.ApplicationName = $"H{appName}";
                });
            }

            // Throttle certain endpoints to protect the website.
            services.AddRateLimiter(_ => _
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 4;
                    options.Window = TimeSpan.FromSeconds(8);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 2;
                }));
        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">Website <see cref="IApplicationBuilder"/>.</param>
        /// <param name="env">Website <see cref="IWebHostEnvironment"/>.</param>
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
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

            app.UseResponseCaching(); // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseRateLimiter();

            app.UseEndpoints(endpoints =>
            {
                // Point to the route that will return the SignalR Hub.
                endpoints.MapHub<LiveEditorHub>("/___cwps_hubs_live_editor");

                endpoints.MapControllerRoute(
                    "MsValidation",
                    ".well-known/microsoft-identity-association.json",
                    new { controller = "Home", action = "GetMicrosoftIdentityAssociation" }).AllowAnonymous();

                endpoints.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "pub",
                    pattern: "pub/{*index}",
                    defaults: new { controller = "Pub", action = "Index" });

                endpoints.MapControllerRoute(
                        "default",
                        "{controller=Home}/{action=Index}/{id?}");

                // Deep path
                endpoints.MapFallbackToController("Index", "Home");

                endpoints.MapRazorPages();
            });
        }

        private static byte[] GetHash(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}