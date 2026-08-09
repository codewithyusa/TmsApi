using System.Threading.RateLimiting;
using System.Threading.Channels;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Console;

using Scalar.AspNetCore;
using Asp.Versioning;
using FluentValidation;
using MediatR;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using TmsApi.Api;
using TmsApi.Api.Options;
using TmsApi.Api.Middleware;
using TmsApi.Api.ExceptionHandlers;
using TmsApi.Api.RateLimiting;
using TmsApi.Api.Hubs;

using TmsApi.Infrastructure.Services;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Persistence.Repositories;
using TmsApi.Infrastructure.Transcripts;
using TmsApi.Infrastructure.Workers;
using TmsApi.Infrastructure.ExternalServices;

using TmsApi.Domain.Entities;

using TmsApi.Application.Interfaces;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Transcripts;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.JsonWriterOptions = new() { Indented = false };
});

// ------------------------------------------------------------
// OpenTelemetry: traces + metrics (Exercise 9 · Step 3)
// ------------------------------------------------------------
const string ServiceName = "tms-api";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: ServiceName, serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddSource(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(m => m
        .AddMeter(ServiceName)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

// Authentication
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>(
        "Training",
        null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddSignalR();

// ------------------------------------------------------------
// CORS: allow the Angular dev server to call this API
// ------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Hybrid Cache
builder.Services.AddHybridCache();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    //
    // Global tier-aware limiter
    //
    options.GlobalLimiter =
        PartitionedRateLimiter.Create<HttpContext, string>(
            httpContext =>
            {
                var (partitionKey, tier) =
                    ApiKeyResolver.Resolve(httpContext);

                return tier switch
                {
                    ApiKeyTier.Paid =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"paid:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 200,
                                TokensPerPeriod = 100,
                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            }),

                    ApiKeyTier.Free =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"free:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 30,
                                TokensPerPeriod = 10,
                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            }),

                    _ =>
                        RateLimitPartition.GetTokenBucketLimiter(
                            $"anonymous:{partitionKey}",
                            _ => new TokenBucketRateLimiterOptions
                            {
                                TokenLimit = 10,
                                TokensPerPeriod = 5,
                                ReplenishmentPeriod =
                                    TimeSpan.FromSeconds(10),
                                QueueLimit = 0,
                                AutoReplenishment = true
                            })
                };
            });

    //
    // Transcript concurrency limiter
    //
    options.AddConcurrencyLimiter(
        "transcripts",
        opt =>
        {
            opt.PermitLimit = 5;
            opt.QueueLimit = 20;
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
        });

    //
    // Search endpoint limiter
    //
    options.AddTokenBucketLimiter(
        "search",
        opt =>
        {
            opt.TokenLimit = 10;
            opt.TokensPerPeriod = 5;
            opt.ReplenishmentPeriod =
                TimeSpan.FromSeconds(10);
            opt.QueueLimit = 2;
            opt.QueueProcessingOrder =
                QueueProcessingOrder.OldestFirst;
            opt.AutoReplenishment = true;
        });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        var retryAfter = "10";

        if (context.Lease.TryGetMetadata(
            MetadataName.RetryAfter,
            out var retry))
        {
            retryAfter =
                ((int)retry.TotalSeconds).ToString();
        }

        context.HttpContext.Response.Headers.RetryAfter =
            retryAfter;

        context.HttpContext.Response.ContentType =
            "application/problem+json";

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Title = "Rate limit exceeded",

                Detail =
                    $"Too many requests. Retry after {retryAfter} seconds.",

                Status =
                    StatusCodes.Status429TooManyRequests,

                Type =
                    "https://tms.local/errors/rate_limit_exceeded"

            },
            ct);
    };
});

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion =
        new ApiVersion(1, 0);

    options.AssumeDefaultVersionWhenUnspecified = true;

    options.ReportApiVersions = true;


    options.ApiVersionReader =
        ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));

})
.AddApiExplorer(options =>
{
    options.GroupNameFormat =
        "'v'VVV";

    options.SubstituteApiVersionInUrl = true;
});

// Services
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<ICourseService, CachedCourseService>();
builder.Services.AddScoped<ICachedCourseService, CachedCourseService>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IGradeService, GradeService>();


// ------------------------------------------------------------
// Polly v8 Resilience Pipeline
// Exercise 8 Step 2
// ------------------------------------------------------------

builder.Services.AddResiliencePipeline(

    "certificate-api",
    pipeline =>
    {
        pipeline

            // Outer: protects against hanging requests
            .AddTimeout(
                TimeSpan.FromSeconds(5))


            // Middle: protects against sustained failures
            .AddCircuitBreaker(
                new CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,

                    MinimumThroughput = 10,

                    SamplingDuration =
                        TimeSpan.FromSeconds(30),

                    BreakDuration =
                        TimeSpan.FromSeconds(15),


                    ShouldHandle =
                        new PredicateBuilder()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>(),


                    OnOpened = args =>
                    {
                        Console.WriteLine(
                            "Circuit OPENED - stopping requests to certificate service");

                        return ValueTask.CompletedTask;
                    },


                    OnClosed = args =>
                    {
                        Console.WriteLine(
                            "Circuit CLOSED - certificate service recovered");

                        return ValueTask.CompletedTask;
                    }
                })


            // Inner: retry only transient failures
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,

                    Delay =
                        TimeSpan.FromMilliseconds(500),

                    BackoffType =
                        DelayBackoffType.Exponential,

                    UseJitter = true,


                    ShouldHandle =
                        new PredicateBuilder()
                            .Handle<HttpRequestException>()
                            .Handle<TimeoutRejectedException>(),


                    OnRetry = args =>
                    {
                        Console.WriteLine(
                            $"Retry #{args.AttemptNumber} after {args.RetryDelay.TotalMilliseconds:F0}ms ({args.Outcome.Exception?.GetType().Name})");

                        return ValueTask.CompletedTask;
                    }
                });
    });

    builder.Services.AddHttpClient<ICertificateService, CertificateService>(
    (sp, client) =>
    {
        var baseUrl =
            sp.GetRequiredService<IConfiguration>()
              .GetValue<string>("TmsApi:PublicBaseUrl")
            ?? "https://localhost:5001";

        client.BaseAddress =
            new Uri(baseUrl);
    });

// Step 4: Simple HTTP client
builder.Services.AddHttpClient("SmsService", client =>
{
    client.BaseAddress =
        new Uri("https://sms.tms.internal");
})
.AddStandardResilienceHandler();


builder.Services.AddSingleton<ITranscriptStatusStore, InMemoryTranscriptStatusStore>();

builder.Services.AddSingleton(
    Channel.CreateUnbounded<TranscriptRequest>());

builder.Services.AddHostedService<TranscriptWorker>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(EnrollStudentCommand).Assembly);


    cfg.AddOpenBehavior(
        typeof(LoggingBehavior<,>));


    cfg.AddOpenBehavior(
        typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(
    typeof(EnrollStudentValidator).Assembly);

// Database
builder.Services.AddDbContext<TmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TmsDatabase"))
    .EnableSensitiveDataLogging()
);

// Options
builder.Services
    .AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddOpenApi();

builder.Services.AddHealthChecks()
    .AddCheck(
        "self",
        () => HealthCheckResult.Healthy("alive"),
        tags: new[] { "live" })
    .AddNpgSql(
        builder.Configuration.GetConnectionString("TmsDatabase")!,
        name: "postgres",
        tags: new[] { "ready" });

var app = builder.Build();

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseExceptionHandler();

app.UseStatusCodePages();


app.UseRouting();

// ------------------------------------------------------------
// CORS middleware must run after UseRouting and before
// UseAuthorization / MapControllers
// ------------------------------------------------------------
app.UseCors("AllowAngular");

// Rate limiter must be after routing
app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<V1DeprecationMiddleware>();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
})
.DisableRateLimiting();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
})
.DisableRateLimiting();

app.MapGet("/api/assessments/results", () =>
{
    return Results.Ok(new
    {
        courseCode = "CS-101",
        studentId = "S-001",
        letterGrade = "A"
    });
})
.RequireAuthorization();

app.MapControllers();

app.MapHub<TmsHub>("/hubs/tms");

var attempts = 0;

app.MapPost("/fake/certificates", async () =>
{
    var n = Interlocked.Increment(ref attempts);


    if (n % 7 == 0)
    {

        await Task.Delay(
            TimeSpan.FromSeconds(20));

        return Results.Ok(new
        {
            Status = "issued",
            Attempt = n
        });
    }


    if (n % 3 != 0)
    {
        return Results.StatusCode(
            StatusCodes.Status503ServiceUnavailable);
    }


    if (n % 11 == 0)
    {
        return Results.BadRequest(new
        {
            error = "validation_failed"
        });
    }


    return Results.Ok(new
    {
        Status = "issued",
        Attempt = n
    });

})
.WithTags("lab-fixtures");


app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing");
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapScalarApiReference();

    using var scope =
        app.Services.CreateScope();

    var context =
        scope.ServiceProvider
            .GetRequiredService<TmsDbContext>();
    await DataSeeder.SeedAsync(context);
}
else
{
    app.UseExceptionHandler();

    app.UseStatusCodePages();
}

using (var scope = app.Services.CreateScope())
{
    var context =
        scope.ServiceProvider
            .GetRequiredService<TmsDbContext>();

    context.Database.Migrate();

    if (!context.Students.Any())
    {
        var students = new List<Student>
        {
            new()
            {
                RegistrationNumber = "TMS-2026-0001",
                Name = "Alice Smith",
                GPA = 3.8m,
                IsActive = true
            },

            new()
            {
                RegistrationNumber = "TMS-2026-0002",
                Name = "Bob Jones",
                GPA = 2.9m,
                IsActive = true
            }
        };

        context.Students.AddRange(students);

        var courses = new List<Course>
        {
            new()
            {
                Code = "CS-101",
                Title = "Introduction to Computer Science",
                MaxCapacity = 30
            },

            new()
            {
                Code = "CS-201",
                Title = "Data Structures and Algorithms",
                MaxCapacity = 25
            }
        };

        context.Courses.AddRange(courses);

        context.SaveChanges();
    }
}

app.Run();