// <copyright file="Program.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>

using System.Threading.RateLimiting;
using Azure.Identity;
using Cosmos.BlobService;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data;
using Cosmos.Common.Data.Logic;
using Cosmos.Common.Services.PowerBI;
using Cosmos.EmailServices;
using Cosmos.MicrosoftGraph;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// Add memory cache for Cosmos data logic and other services.
builder.Services.AddMemoryCache();

// Create one instance of the DefaultAzureCredential to be used throughout the application.
var defaultAzureCredential = new DefaultAzureCredential();
builder.Services.AddSingleton(defaultAzureCredential);

// Add Graph services
builder.Services.AddScoped<MsGraphService>();
builder.Services.AddScoped<MsGraphClaimsTransformation>();

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

// Get the DB connection string.
var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string is missing.");
}

// Name of the Cosmos database to use
var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");
if (string.IsNullOrEmpty(cosmosIdentityDbName))
{
    cosmosIdentityDbName = "cosmoscms";
}

// Add the Cosmos database context here
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

// Add the BLOB and File Storage contexts for Cosmos
builder.Services.AddCosmosStorageContext(builder.Configuration);

var container = Cosmos.BlobService.ServiceCollectionExtensions.GetBlobContainerClient(builder.Configuration, defaultAzureCredential, "pkyes");
_ = container.CreateIfNotExistsAsync().Result;

builder.Services.AddDataProtection().PersistKeysToAzureBlobStorage(container.GetBlobClient("keys.xml"));

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// Add the Microsoft Graph services
builder.Services.Configure<MicrosoftIdentityOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Events.OnTokenValidated = async context =>
    {
        if (context.Principal != null)
        {
            var claimsTransformation = context.HttpContext.RequestServices.GetRequiredService<MsGraphClaimsTransformation>();
            context.Principal = await claimsTransformation.TransformAsync(context.Principal);
        }
    };
});

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

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
builder.Services.AddRazorPages()
    .AddMicrosoftIdentityUI();

// Add authorization handler
builder.Services.AddSingleton<IAuthorizationHandler, HandlerUsingAzureGroups>();
var userGroup = builder.Configuration.GetValue<string>("AzureAd:UserGroup");
if (string.IsNullOrEmpty(userGroup))
{
    throw new InvalidOperationException("User group id is missing.");
}

var adminGroup = builder.Configuration.GetValue<string>("AzureAd:AdminGroup");

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserPolicy", policy =>
    {
        policy.Requirements.Add(new GroupAuthorizationRequirement(userGroup));
    });

    if (!string.IsNullOrEmpty(adminGroup))
    {
        options.AddPolicy("AdminPolicy", policy =>
        {
            policy.Requirements.Add(new GroupAuthorizationRequirement(adminGroup));
        });
    }
});

// Add Email services
builder.Services.AddCosmosEmailServices(builder.Configuration);

var app = builder.Build();

// BEGIN
// https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
app.UseForwardedHeaders();

// END

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();

// Add Rate Limiter to prevent abuse.
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
