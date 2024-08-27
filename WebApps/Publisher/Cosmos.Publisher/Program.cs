// <copyright file="Program.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Threading.RateLimiting;
using AspNetCore.Identity.CosmosDb.Extensions;
using Azure.Storage.Blobs;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Services.Configurations;
using Cosmos.Common.Services.PowerBI;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;

/// <summary>
/// Main program.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApplicationInsightsTelemetry();

        // Add CORS
        // See: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
        var corsOrigins = builder.Configuration.GetValue<string>("CorsAllowedOrigins");
        if (string.IsNullOrEmpty(corsOrigins))
        {
            builder.Services.AddCors();
        }
        else
        {
            var origins = corsOrigins.Split(',');
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(
                    name: "AllowedOrigPolicy",
                    policy =>
                    {
                        policy.WithOrigins(origins);
                    });
            });
        }

        // Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");

        // Name of the Cosmos database to use
        var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");
        if (string.IsNullOrEmpty(cosmosIdentityDbName))
        {
            cosmosIdentityDbName = "cosmoscms";
        }

        // Add the Cosmos database context here
        var cosmosRegionName = builder.Configuration.GetValue<string>("CosmosRegionName");
        if (string.IsNullOrEmpty(cosmosRegionName))
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
              options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName));
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseCosmos(connectionString: connectionString, databaseName: cosmosIdentityDbName, cosmosOps => cosmosOps.Region(cosmosRegionName));
            });
        }

        // Add the BLOB and File Storage contexts for Cosmos
        builder.Services.AddCosmosStorageContext(builder.Configuration);

        // Add shared data protection here
        var blobConnection = builder.Configuration.GetConnectionString("AzureBlobStorageConnectionString");
        var container = new BlobContainerClient(blobConnection, "pkyes");
        container.CreateIfNotExists();
        builder.Services.AddDataProtection().PersistKeysToAzureBlobStorage(container.GetBlobClient("keys.xml"));

        builder.Services.AddMvc()
                        .AddNewtonsoftJson(options =>
                            options.SerializerSettings.ContractResolver =
                                new DefaultContractResolver());

        // Add Cosmos Identity here
        builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
              options => options.SignIn.RequireConfirmedAccount = true)
            .AddDefaultUI() // Use this if Identity Scaffolding added
            .AddDefaultTokenProviders();

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
            });
        }

        // Add Power BI Token Service.
        builder.Services.AddScoped(typeof(PowerBiTokenService));
        builder.Services.Configure<PowerBiAuth>(builder.Configuration.GetSection("PowerBiAuth"));

        // https://docs.microsoft.com/en-us/aspnet/core/security/authentication/accconfirm?view=aspnetcore-3.1&tabs=visual-studio
        builder.Services.ConfigureApplicationCookie(o =>
        {
            o.Cookie.Name = "CosmosAuthCookie";
            o.ExpireTimeSpan = TimeSpan.FromDays(5);
            o.SlidingExpiration = true;
        });

        // Add IDistributed cache using Cosmos DB
        // This enables the editor to run in a web farm without needing
        // the "sticky bit" set.
        // See: https://github.com/Azure/Microsoft.Extensions.Caching.Cosmos
        builder.Services.AddCosmosCache((cacheOptions) =>
        {
            cacheOptions.ContainerName = "PublisherCache";
            cacheOptions.DatabaseName = cosmosIdentityDbName;
            cacheOptions.ClientBuilder = new CosmosClientBuilder(connectionString);
            cacheOptions.CreateIfNotExists = true;
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

        // Throttle certain endpoints to protect the website.
        builder.Services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: "fixed", options =>
            {
                options.PermitLimit = 4;
                options.Window = TimeSpan.FromSeconds(8);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            }));

        // END
        builder.Services.AddResponseCaching();

        // Get the boot variables loaded, and
        // do some validation to make sure Cosmos can boot up
        // based on the values given.
        var cosmosStartup = new CosmosStartup(builder.Configuration);

        // Add Cosmos Options
        var option = cosmosStartup.Build();

        builder.Services.AddSingleton(option);
        builder.Services.AddTransient<ArticleLogic>();

        builder.Services.AddControllersWithViews();

        // Add Email services
        builder.Services.AddCosmosEmailServices(builder.Configuration);

        var app = builder.Build();

        // BEGIN
        // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
        app.UseForwardedHeaders();

        // END

        // Middle-ware proper order:
        // See: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-6.0#middleware-order
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");

            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();

        app.UseRouting();

        app.UseRateLimiter();

        if (string.IsNullOrEmpty(corsOrigins))
        {
            // See: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
            app.UseCors();
        }
        else
        {
            app.UseCors("AllowedOrigPolicy");
        }

        app.UseResponseCaching(); // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "pub",
            pattern: "pub/{*index}",
            defaults: new { controller = "Pub", action = "Index" });

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.MapControllerRoute(
                    "MsValidation",
                    ".well-known/microsoft-identity-association.json",
                    new { controller = "Home", action = "GetMicrosoftIdentityAssociation" });

        app.MapControllerRoute(
                    "MyArea",
                    "{area:exists}/{controller=Home}/{action=Index}/{id?}");

        app.MapFallbackToController("Index", "Home");

        app.MapRazorPages();

        app.Run();
    }
}