// <copyright file="Startup.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using System.Web;
using AspNetCore.Identity.CosmosDb.Extensions;
using Azure.Identity;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Cms.Hubs;
using Cosmos.Cms.Services;
using Cosmos.Common.Data;
using Cosmos.Common.Services;
using Cosmos.Common.Services.Configurations;
using Cosmos.Common.Services.PowerBI;
using Cosmos.ConnectionStrings;
using Cosmos.Editor.Data.Logic;
using Cosmos.Editor.Services;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
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

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IConnectionStringProvider, ConnectionStringProvider>();

// Add the Cosmos database context here
if (option.Value.SiteSettings.MultiTenantEditor)
{
    builder.Services.AddDbContext<ApplicationDbContext>();
}
else
{
    // The Cosmos connection string
    var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");

    // Name of the Cosmos database to use
    var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");
    if (string.IsNullOrEmpty(cosmosIdentityDbName))
    {
        cosmosIdentityDbName = "cosmoscms";
    }

    // Add the Cosmos database context here for single tenant setup.
    var cosmosRegionName = builder.Configuration.GetValue<string>("CosmosRegionName");
    var conpartsDict = connectionString.Split(";").Where(w => !string.IsNullOrEmpty(w)).Select(part => part.Split('=')).ToDictionary(sp => sp[0], sp => sp[1]);
    var endpoint = conpartsDict["AccountEndpoint"];

    builder.Services.AddDbContext<ApplicationDbContext>(
        options =>
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
        }, optionsLifetime: ServiceLifetime.Singleton);
}

// Add Cosmos Identity here
builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
      options => options.SignIn.RequireConfirmedAccount = true)
    .AddDefaultUI() // Use this if Identity Scaffolding added
    .AddDefaultTokenProviders();

// Add shared data protection here
//var containerClient = Cosmos.BlobService.ServiceCollectionExtensions.GetBlobContainerClient(builder.Configuration, new DefaultAzureCredential(), "dataprotection");
//containerClient.CreateIfNotExists();

//builder.Services.AddDataProtection()
//    .UseCryptographicAlgorithms(
//    new AuthenticatedEncryptorConfiguration
//    {
//        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
//        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
//    })
//    .PersistKeysToAzureBlobStorage(containerClient.GetBlobClient("editorkeys.xml"));

// ===========================================================
// SUPPORTED OAuth Providers

//-------------------------------
// Add Google if keys are present
var googleOAuth = builder. Configuration.GetSection("GoogleOAuth").Get<OAuth>();

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

// Add Power BI Token Service.
builder.Services.AddScoped(typeof(PowerBiTokenService));
builder.Services.Configure<PowerBiAuth>(builder.Configuration.GetSection("PowerBiAuth"));

// Add Azure CDN/Frontdoor configuration here.
builder.Services.Configure<CdnService>(builder.Configuration.GetSection("AzureCdnConfig"));

// Add Email services
builder.Services.AddCosmosEmailServices(builder.Configuration);

// Add the BLOB and File Storage contexts for Cosmos
builder.Services.AddCosmosStorageContext(builder.Configuration);

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
builder.Services.AddResponseCaching();

// Add the SignalR service.
// If there is a DB connection, then use SQL backplane.
// See: https://github.com/IntelliTect/IntelliTect.AspNetCore.SignalR.SqlServer
var signalRConnection = builder.Configuration.GetConnectionString("CosmosSignalRConnection");
if (string.IsNullOrEmpty(signalRConnection))
{
    builder.Services.AddSignalR();
}
else
{
    var appUli = new Uri(option.Value.SiteSettings.PublisherUrl);
    var sendpoint = new Microsoft.Azure.SignalR.ServiceEndpoint(signalRConnection);
    var appName = Cosmos.Editor.ProgramHelper.GetHashString(appUli.DnsSafeHost);

    builder.Services.AddSignalR().AddAzureSignalR(config =>
    {
        config.Endpoints = new[] { sendpoint };
        config.ApplicationName = $"H{appName}";
    });
}

// Throttle certain endpoints to protect the website.
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(8);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

var app = builder.Build();

// Domain middleware used to get the domain name of the current request.
app.UseMiddleware<DomainMiddleware>();

if (option.Value.SiteSettings.StaticWebPages)
{
    // Get the static files copied over to blob storage as needed.
    var env = app.Services.GetRequiredService<IWebHostEnvironment>();
    var storageContext = app.Services.GetRequiredService<StorageContext>();

    // Copy the required static pages to the blob storage.
    var ckeditorFile = "lib/ckeditor/ckeditor5-content.css";
    
    var path = Path.Combine(env.WebRootPath, ckeditorFile);
    using var r = new StreamReader(path);
    using var memStream = new MemoryStream(Encoding.UTF8.GetBytes(await r.ReadToEndAsync()));

    storageContext.AppendBlob(memStream, new Cosmos.BlobService.Models.FileUploadMetaData()
    {
        ContentType = "text/css",
        FileName = "ckeditor5-content.css",
        ChunkIndex = 0,
        TotalChunks = 1,
        TotalFileSize = memStream.Length,
        UploadUid = Guid.NewGuid().ToString(),
        RelativePath = "lib/ckeditor/ckeditor5-content.css"
    });
}

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

app.UseResponseCaching(); // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1

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
        "default",
        "{controller=Home}/{action=Index}/{id?}");

// Deep path
app.MapFallbackToController("Index", "Home");

app.MapRazorPages();

await app.RunAsync();