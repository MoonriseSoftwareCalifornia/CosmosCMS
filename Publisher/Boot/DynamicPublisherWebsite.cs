// <copyright file="DynamicPublisherWebsite.cs" company="Moonrise Software, LLC">
// Copyright (c) Moonrise Software, LLC. All rights reserved.
// Licensed under the GNU Public License, Version 3.0 (https://www.gnu.org/licenses/gpl-3.0.html)
// See https://github.com/MoonriseSoftwareCalifornia/CosmosCMS
// for more information concerning the license and the contributors participating to this project.
// </copyright>
namespace Cosmos.Publisher.Boot
{
    using System.Text.RegularExpressions;
    using System.Threading.RateLimiting;
    using AspNetCore.Identity.FlexDb.Extensions;
    using Azure.Identity;
    using Cosmos.BlobService;
    using Cosmos.Cms.Common.Services.Configurations;
    using Cosmos.Common.Data;
    using Cosmos.Common.Data.Logic;
    using Cosmos.EmailServices;
    using Cosmos.MicrosoftGraph;
    using Microsoft.AspNetCore.Antiforgery;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.RateLimiting;
    using Microsoft.Azure.Cosmos.Fluent;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Configures and initializes the web application with the necessary services, middleware, and settings.
    /// </summary>
    /// <remarks>This method sets up the application by registering essential services, configuring
    /// middleware, and applying application-specific settings. It includes support for memory caching, Azure
    /// credentials, Application Insights, CORS policies, Cosmos DB integration, identity management, OAuth providers,
    /// distributed caching, rate limiting, and more. The method also configures the HTTP request pipeline and maps
    /// routes for controllers and Razor Pages.  Key features include: - Integration with Cosmos DB for data storage and
    /// caching. - Support for OAuth authentication with Google and Microsoft providers. - Middleware configuration for
    /// HTTPS redirection, static files, response caching, and rate limiting. - Customizable CORS policies and cookie
    /// settings. - Support for distributed environments with data protection and caching mechanisms.  This method is
    /// intended to be called during the application's startup process to ensure all required dependencies and
    /// configurations are in place before the application begins handling requests.</remarks>
    public static class DynamicPublisherWebsite
    {
        /// <summary>
        /// Boots the web application by configuring services, middleware, and settings.
        /// </summary>
        /// <param name="builder">Web application service builder.</param>
        /// <returns>Task.</returns>
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

            // Get the DB connection string.
            var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                var keys = builder.Configuration.AsEnumerable().Select(keys => keys.Key).Where(w => w.StartsWith("ConnectionStrings", StringComparison.CurrentCultureIgnoreCase)).ToArray();
                var keyString = string.Join(", ", keys);
                throw new InvalidOperationException("Connection string is missing. Found keys: " + keyString);
            }

            // Name of the Cosmos database to use
            var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");
            if (string.IsNullOrEmpty(cosmosIdentityDbName))
            {
                cosmosIdentityDbName = "cosmoscms";
            }

            var isStaticWebsite =
                builder.Configuration.GetValue<bool?>("CosmosStaticWebPages") ?? false;

            // Add the Cosmos database context here
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseCosmos(connectionString, cosmosIdentityDbName);
            });

            // Add the BLOB and File Storage contexts for Cosmos
            builder.Services.AddCosmosStorageContext(builder.Configuration);

            // Add shared data protection here
            var containerClient = Cosmos.BlobService.ServiceCollectionExtensions.GetBlobContainerClient(builder.Configuration, new DefaultAzureCredential(), "dataprotection");
            containerClient.CreateIfNotExists();

            // Add shared data protection here
            builder.Services.AddCosmosCmsDataProtection(builder.Configuration, defaultAzureCredential);

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
            var googleOAuth = builder.Configuration.GetSection("GoogleOAuth").Get<Cosmos.Common.Services.Configurations.OAuth>();

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
            var entraIdOAuth = builder.Configuration.GetSection("MicrosoftOAuth").Get<Cosmos.Common.Services.Configurations.OAuth>();

            if (entraIdOAuth != null && entraIdOAuth.IsConfigured())
            {
                // Add Graph services
                builder.Services.AddScoped<MsGraphService>();
                builder.Services.AddScoped<MsGraphClaimsTransformation>();

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

            app.MapGet("ccms__antiforgery/token", (IAntiforgery forgeryService, HttpContext context) =>
            {
                var tokens = forgeryService.GetAndStoreTokens(context);
                context.Response.Headers["XSRF-TOKEN"] = tokens.RequestToken;
                return Results.Ok();
            });

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

            await app.RunAsync();
        }
    }
}
