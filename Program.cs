using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// =======================
// SERVICES
// =======================

// Authentication
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// Controllers (Exercise 5)
builder.Services.AddControllers();

// Dependency Injection
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// =======================
// OPTIONS (Exercise 3)
// =======================

builder.Services
    .AddOptions<PaymentOptions>()
    .BindConfiguration("Payments")
    .ValidateDataAnnotations()
    .ValidateOnStart();

// =======================
// PROBLEMD DETAILS (Exercise 6)
// =======================

builder.Services.AddProblemDetails();

// =======================
// OPENAPI (Exercise 7 - NEW)
// =======================

builder.Services.AddOpenApi();

// =======================
// APP BUILD
// =======================

var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================

// Logging Middleware (Session 1)
app.UseMiddleware<RequestLoggingMiddleware>();

// Exception Handling (ProblemDetails)
app.UseExceptionHandler();

// Optional: consistent 404 JSON
app.UseStatusCodePages();

// Routing
app.UseRouting();

// Authentication
app.UseAuthentication();

// Authorization
app.UseAuthorization();

// =======================
// ENDPOINTS
// =======================

// Exercise 1 protected endpoint
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

// Exercise 5 Controllers
app.MapControllers();

// =======================
// TEST ERROR ROUTE (Exercise 6)
// =======================

app.MapGet("/api/error", () =>
{
    throw new TmsDatabaseException(
        "Simulated database failure for ProblemDetails testing"
    );
});

// =======================
// ENVIRONMENT TOGGLE (Exercise 7)
// =======================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseExceptionHandler();
}

app.Run();