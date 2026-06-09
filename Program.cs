using Microsoft.AspNetCore.Authentication;

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
// APP BUILD
// =======================

var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================

// Logging Middleware (Session 1)
app.UseMiddleware<RequestLoggingMiddleware>();

// Exception Handling
app.UseExceptionHandler("/error");

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

app.Run();