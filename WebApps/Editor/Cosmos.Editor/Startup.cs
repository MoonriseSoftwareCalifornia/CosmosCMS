// <copyright file="Startup.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

namespace Cosmos.Cms
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using AspNetCore.Identity.CosmosDb.Extensions;
    using Azure.Identity;
    using Azure.ResourceManager;
    using Azure.Storage.Blobs;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Cms.Data.Logic;
    using Cosmos.Cms.Hubs;
    using Cosmos.Cms.Services;
    using Cosmos.Common.Data;
    using Cosmos.Editor.Models;
    using Cosmos.EmailServices;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
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
                throw new Exception("STARTUP: ApplicationDbContextConnection is null or empty.");
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
            // var setupCosmosDb = Configuration.GetValue<bool?>("SetupCosmosDb");

            // If the following is set, it will create the Cosmos database and
            //  required containers.
            if (option.Value.SiteSettings.AllowSetup)
            {
                try
                {
                    var builder1 = new DbContextOptionsBuilder<ApplicationDbContext>();
                    builder1.UseCosmos(connectionString, cosmosIdentityDbName);

                    using (var dbContext = new ApplicationDbContext(builder1.Options))
                    {
                        dbContext.Database.EnsureCreated();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("STARTUP: Could not initialize database with error.", e);
                }
            }

            // Add the Cosmos database context here
            var cosmosRegionName = Configuration.GetValue<string>("CosmosRegionName");
            if (string.IsNullOrEmpty(cosmosRegionName))
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                  options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName, cosmosOps => cosmosOps.Region(cosmosRegionName));
                });
            }

            // Add Cosmos Identity here
            services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
                  options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultUI() // Use this if Identity Scaffolding added
                .AddDefaultTokenProviders();

            // Add shared data protection here
            var blobConnection = Configuration.GetConnectionString("AzureBlobStorageConnectionString");
            if (string.IsNullOrEmpty(blobConnection))
            {
                throw new Exception("STARTUP: AzureBlobStorageConnectionString is null or empty");
            }

            var container = new BlobContainerClient(blobConnection, "ekyes");
            container.CreateIfNotExists();
            services.AddDataProtection().PersistKeysToAzureBlobStorage(container.GetBlobClient("keys.xml"));

            // SUPPORTED OAuth Providers
            // Add Google if keys are present
            var googleClientId = Configuration["Authentication_Google_ClientId"];
            var googleClientSecret = Configuration["Authentication_Google_ClientSecret"];

            if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
            {
                services.AddAuthentication().AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }

            // Add Microsoft if keys are present
            var microsoftClientId = Configuration["Authentication_Microsoft_ClientId"];
            var microsoftClientSecret = Configuration["Authentication_Microsoft_ClientSecret"];

            if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
            {
                services.AddAuthentication().AddMicrosoftAccount(options =>
                {
                    options.ClientId = microsoftClientId;
                    options.ClientSecret = microsoftClientSecret;
                });
            }

            // Add Azure Frontdoor connection here
            // First try and get the connection from a configuration variable
            var azureFrontdoorConnection = Configuration.GetValue<FrontDoorConnection>("FrontdoorConnection");

            if (azureFrontdoorConnection == null)
            {
                azureFrontdoorConnection = Configuration.GetSection("FrontdoorConnection").Get<FrontDoorConnection>();
            }

            if (azureFrontdoorConnection == null)
            {
                azureFrontdoorConnection = new FrontDoorConnection();
            }
            else
            {
                azureFrontdoorConnection.EndpointName = Configuration["FrontdoorEndpointName"];
            }

            services.AddSingleton(azureFrontdoorConnection);

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromSeconds(3600);
                options.Cookie.IsEssential = true;
            });

            // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
            services.ConfigureApplicationCookie(o =>
            {
                o.Cookie.Name = "CosmosAuthCookie";
                o.ExpireTimeSpan = TimeSpan.FromDays(5);
                o.SlidingExpiration = true;
            });

            var azureSubscription = new AzureSubscription();

            // Get the Azure app service connection to control Front Door and CDNs.
            var tenantId = Configuration["AzureServices_App_TenantId"];
            var clientId = Configuration["AzureServices_App_ClientId"];
            var clientSecret = Configuration["AzureServices_App_Secret"];
            if (!string.IsNullOrEmpty(tenantId) &&
                !string.IsNullOrEmpty(clientId) &&
                !string.IsNullOrEmpty(clientSecret))
            {
                var armClient = new ArmClient(new ClientSecretCredential(tenantId, clientId, clientSecret));
                azureSubscription.Subscription = armClient.GetDefaultSubscription();
                
            }

            services.AddSingleton(azureSubscription);

            // Add services
            var azureCommunicationConnection = Configuration.GetConnectionString("AzureCommunicationConnection");

            if (azureCommunicationConnection == null)
            {
                // Email provider
                var sendGridApiKey = Configuration.GetValue<string>("CosmosSendGridApiKey");
                var adminEmail = "DoNotReply@cosmosws.io";
                if (!string.IsNullOrEmpty(sendGridApiKey) && !string.IsNullOrEmpty(adminEmail))
                {
                    var sendGridOptions = new SendGridEmailProviderOptions(sendGridApiKey, adminEmail);
                    services.AddSendGridEmailProvider(sendGridOptions);
                }
            }
            else
            {
                services.AddAzureCommunicationEmailSenderProvider(new AzureCommunicationEmailProviderOptions()
                {
                    ConnectionString = azureCommunicationConnection,
                    DefaultFromEmailAddress = "DoNotReply@cosmosws.io"
                });
            }

            // End add SendGrid

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
                    // options.AllowAreas = true;
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
                    options.Conventions.AuthorizeAreaPage("Identity", "/Account/Logout");
                });

            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-2.1&tabs=visual-studio#http-strict-transport-security-protocol-hsts
            services.AddHsts(options =>
            {
                options.Preload = true;
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(365);

                // options.ExcludedHosts.Add("example.com");
                // options.ExcludedHosts.Add("www.example.com");
            });

            services.ConfigureApplicationCookie(options =>
            {
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
            // app.UseForwardedHeaders();
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

            // https://docs.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/5.0/middleware-database-error-page-obsolete
            // services.AddDatabaseDeveloperPageExceptionFilter();

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
                var endpoint = new Microsoft.Azure.SignalR.ServiceEndpoint(signalRConnection);
                var appName = GetHashString(appUli.DnsSafeHost);

                services.AddSignalR().AddAzureSignalR(config =>
                {
                    config.Endpoints = new[] { endpoint };
                    config.ApplicationName = $"H{appName}";
                });
            }
        }

        private static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        private static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
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
            // BEGIN
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();

            // END
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
    }
}