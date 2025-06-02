using Azure.Identity;
using Cosmos.Common.Data;
using Cosmos.DynamicConfig;
using Cosmos.EmailServices;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add memory cache for Cosmos data logic and other services.
builder.Services.AddMemoryCache();

// Create one instance of the DefaultAzureCredential to be used throughout the application.
var defaultAzureCredential = new DefaultAzureCredential();
builder.Services.AddSingleton(defaultAzureCredential);

// The following line enables Application Insights telemetry collection.
// See: https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core?tabs=netcore6
builder.Services.AddApplicationInsightsTelemetry();

// The next two services have to appear before DB Context.
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IDynamicConfigurationProvider, DynamicConfigurationProvider>();

// Add database context.
builder.Services.AddTransient((serviceProvider) =>
{
    return new ApplicationDbContext(serviceProvider);
});

// Add Email services
builder.Services.AddCosmosEmailServices(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Throttle certain endpoints to protect the website.
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(8);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

// Anti-forgery token service
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; // Custom header name for the anti-forgery token
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.MapGet("/", () => "Hello world!");

// Domain middleware used to get the domain name of the current request.
app.UseMiddleware<DomainMiddleware>();
app.UseHttpsRedirection();

app.UseAuthorization();

// Add Rate Limiter to prevent abuse.
app.MapControllers();

app.Run();
