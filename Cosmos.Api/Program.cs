using Azure.Identity;
using Cosmos.BlobService;
using Cosmos.Common.Data;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos.Fluent;
using Cosmos.Cms.Common.Services.Configurations;
using Cosmos.Common.Data.Logic;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add memory cache for Cosmos data logic and other services.
builder.Services.AddMemoryCache();

// Create one instance of the DefaultAzureCredential to be used throughout the application.
var defaultAzureCredential = new DefaultAzureCredential();
builder.Services.AddSingleton(defaultAzureCredential);

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

// Add shared data protection here
builder.Services.AddDataProtection()
    .SetApplicationName("api").UseCryptographicAlgorithms(
    new AuthenticatedEncryptorConfiguration
    {
        EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
        ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
    }).PersistKeysToDbContext<ApplicationDbContext>();

// Add services to the container.

builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver =
                        new DefaultContractResolver()); ;

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

// Throttle certain endpoints to protect the website.
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(8);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddResponseCaching();

// Get the boot variables loaded, and
// do some validation to make sure Cosmos can boot up
// based on the values given.
var cosmosStartup = new CosmosStartup(builder.Configuration);

// Add Email services
builder.Services.AddCosmosEmailServices(builder.Configuration);

// Add Cosmos Options
var option = cosmosStartup.Build();

builder.Services.AddSingleton(option);
builder.Services.AddTransient<ArticleLogic>();

// Build the app now.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

// https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-3.1
app.UseResponseCaching(); 

app.UseAuthorization();

app.MapControllers();

app.Run();
