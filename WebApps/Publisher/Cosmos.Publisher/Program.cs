// <copyright file="Program.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using AspNetCore.Identity.CosmosDb.Extensions;
using Azure.Storage.Blobs;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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
#pragma warning restore CS8604 // Possible null reference argument.

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
        builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole>(
              options => options.SignIn.RequireConfirmedAccount = true)
            .AddDefaultUI() // Use this if Identity Scaffolding added
            .AddDefaultTokenProviders();

        // SUPPORTED OAuth Providers
        // Add Google if keys are present
        var googleClientId = builder.Configuration["Authentication_Google_ClientId"];
        var googleClientSecret = builder.Configuration["Authentication_Google_ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            builder.Services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
            });
        }

        // Add Microsoft if keys are present
        var microsoftClientId = builder.Configuration["Authentication_Microsoft_ClientId"];
        var microsoftClientSecret = builder.Configuration["Authentication_Microsoft_ClientSecret"];
        if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
        {
            builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftClientId;
                options.ClientSecret = microsoftClientSecret;
            });
        }

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
            cacheOptions.ContainerName = "EditorCache";
            cacheOptions.DatabaseName = cosmosIdentityDbName;
            cacheOptions.ClientBuilder = new CosmosClientBuilder(connectionString);
            cacheOptions.CreateIfNotExists = true;
        });

        // Get the boot variables loaded, and
        // do some validation to make sure Cosmos can boot up
        // based on the values given.
        var cosmosStartup = new CosmosStartup(builder.Configuration);

        // Add Cosmos Options
        var option = cosmosStartup.Build();

        builder.Services.AddSingleton(option);
        builder.Services.AddTransient<ArticleLogic>();

        builder.Services.AddControllersWithViews();

        // Email provider
        //
        // Add services
        var azureCommunicationConnection = builder.Configuration.GetConnectionString("AzureCommunicationConnection");

        if (azureCommunicationConnection == null)
        {
            // Email provider
            var sendGridApiKey = builder.Configuration.GetValue<string>("CosmosSendGridApiKey");
            var adminEmail = "DoNotReply@cosmosws.io";
            if (!string.IsNullOrEmpty(sendGridApiKey) && !string.IsNullOrEmpty(adminEmail))
            {
                var sendGridOptions = new SendGridEmailProviderOptions(sendGridApiKey, adminEmail);
                builder.Services.AddSendGridEmailProvider(sendGridOptions);
            }
        }
        else
        {
            builder.Services.AddAzureCommunicationEmailSenderProvider(new AzureCommunicationEmailProviderOptions()
            {
                ConnectionString = azureCommunicationConnection,
                DefaultFromEmailAddress = "DoNotReply@cosmosws.io"
            });
        }

        var app = builder.Build();

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

        if (string.IsNullOrEmpty(corsOrigins))
        {
            // See: https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-6.0
            app.UseCors();
        }
        else
        {
            app.UseCors("AllowedOrigPolicy");
        }

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