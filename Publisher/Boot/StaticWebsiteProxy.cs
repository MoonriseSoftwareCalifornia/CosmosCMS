// <copyright file="StaticWebsiteProxy.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>
namespace Cosmos.Publisher.Boot
{
    using System.Threading.RateLimiting;
    using Azure.Identity;
    using Cosmos.BlobService;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.RateLimiting;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Configures and starts a web application to serve static files from the "wwwroot" directory.
    /// </summary>
    /// <remarks>This method sets up the application to serve static files and enables default file handling, 
    /// such as serving "index.html" when a directory is accessed. Directory browsing is also enabled  to allow users to
    /// view the contents of directories if no default file is present.</remarks>
    public static class StaticWebsiteProxy
    {
        /// <summary>
        /// Configures and starts the web application with support for serving static files.
        /// </summary>
        /// <remarks>This method enables serving static files from the "wwwroot" directory and configures
        /// the application  to look for default files such as "index.html". Directory browsing is also enabled to allow
        /// users  to view directory contents if no default file is present. Once configured, the application is
        /// started.</remarks>
        /// <param name="builder">The <see cref="WebApplicationBuilder"/> used to configure and build the application.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task Boot(WebApplicationBuilder builder)
        {
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

            // Add the BLOB and File Storage contexts for Cosmos
            builder.Services.AddCosmosStorageContext(builder.Configuration);

            // Throttle certain endpoints to protect the website.
            builder.Services.AddRateLimiter(_ => _
                .AddFixedWindowLimiter(policyName: "fixed", options =>
                {
                    options.PermitLimit = 4;
                    options.Window = TimeSpan.FromSeconds(8);
                    options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    options.QueueLimit = 2;
                }));

            // Configure Newtonsoft.Json to use DefaultContractResolver for property names
            builder.Services.AddMvc()
                            .AddNewtonsoftJson(options =>
                                options.SerializerSettings.ContractResolver =
                                    new DefaultContractResolver());

            // Enable response caching
            builder.Services.AddResponseCaching(options =>
            {
                options.MaximumBodySize = 1024;
                options.UseCaseSensitivePaths = true;
            });

            // Add services for controllers with views
            builder.Services.AddControllersWithViews();

            // Configure the application to forward headers when behind a proxy
            var app = builder.Build();

            // Use forwarded headers to correctly handle HTTPS redirection and other headers when behind a proxy
            // https://seankilleen.com/2020/06/solved-net-core-azure-ad-in-docker-container-incorrectly-uses-an-non-https-redirect-uri/
            app.UseForwardedHeaders();

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

            app.Use(async (context, next) =>
            {
                context.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(30)
                    };
                context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.Vary] =
                    new string[] { "Accept-Encoding" };

                await next();
            });

            app.MapGet("ccms__antiforgery/token", (IAntiforgery forgeryService, HttpContext context) =>
            {
                var tokens = forgeryService.GetAndStoreTokens(context);
                context.Response.Headers["XSRF-TOKEN"] = tokens.RequestToken;
                return Results.Ok();
            });

            app.MapControllerRoute(
                name: "catchall",
                pattern: "{*path}",
                defaults: new { controller = "StaticProxy", action = "Index" });

            await app.RunAsync();
        }
    }
}
