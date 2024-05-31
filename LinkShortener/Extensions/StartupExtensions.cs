using LinkShortener.Database;
using LinkShortener.Options;
using LinkShortener.Services;
using LinkShortener.Utilities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LinkShortener.Extensions
{
    public static class StartupExtensions
    {
        public static void SetupDatabase(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.ConfigureOptions<DatabaseOptionsSetup>();

            builder.Services.AddDbContext<ApplicationDbContext>(
                (serviceProvider, dbContextOptionsBuilder) =>
                {
                    var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>()!.Value;

                    dbContextOptionsBuilder.UseNpgsql(
                    databaseOptions.ConnectionString,
                    sqlServerAction =>
                    {
                        sqlServerAction.EnableRetryOnFailure(databaseOptions.MaxRetryCount);
                        sqlServerAction.CommandTimeout(databaseOptions.CommandTimeout);
                    });

                    if (builder.Environment.IsDevelopment())
                    {
                        dbContextOptionsBuilder.EnableDetailedErrors(databaseOptions.EnableDetailedErrors);
                        dbContextOptionsBuilder.EnableSensitiveDataLogging(databaseOptions.EnableSensitiveDatalogging);
                    }
                });

            builder.Services.AddStackExchangeRedisCache(options =>
                options.Configuration = builder.Configuration.GetConnectionString("Cache"));
        }

        public static void SetupSwagger(this IHostApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "LinkShortener API",
                    Description = "An ASP.NET Core Web API to Shorten Links",
                    TermsOfService = new Uri("https://example.com/terms"),
                    Contact = new OpenApiContact
                    {
                        Name = "Peter Riesz",
                        Url = new Uri("https://example.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "Free to use",
                        Url = new Uri("https://example.com/license")
                    }
                });
            });
        }

        public static void SetupOpenTelemetry(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Logging.AddOpenTelemetry(x =>
            {
                x.IncludeScopes = true;
                x.IncludeFormattedMessage = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(x =>
                {
                    x.AddRuntimeInstrumentation()
                        .AddMeter(
                            "Microsoft.AspNetCore.Hosting",
                            "Microsoft.AspNetCore.Server.Kestrel",
                            "System.Net.Http",
                            "LinkShortener.Api"
                        );
                })
                .WithTracing(x =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        x.SetSampler<AlwaysOnSampler>();
                    }

                    x.AddAspNetCoreInstrumentation()
                     .AddHttpClientInstrumentation();
                });

            builder.Services.Configure<OpenTelemetryLoggerOptions>(logging => logging.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryMeterProvider(metrics => metrics.AddOtlpExporter());
            builder.Services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddOtlpExporter());

            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
            });

            builder.Services.AddMetrics();
            builder.Services.AddSingleton<LinkShortenerMetrics>();
        }

        public static void SetupCors(this WebApplicationBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy",
                    builder => builder
                        .WithOrigins("http://localhost:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });
        }

        public static void RegisterServices(this WebApplicationBuilder builder)
        {
            builder?.Services.AddScoped<UrlShorteningService>();
        }

        public static void SetupDevelopmentMode(this WebApplication app)
        {
            ArgumentNullException.ThrowIfNull(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.ApplyMigrations();
            }
        }
    }
}
