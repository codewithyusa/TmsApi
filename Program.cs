using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// =======================
// SERVICES
// =======================

// Auth
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

// DI (IMPORTANT FOR SESSION 2)
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();

// =======================
// APP BUILD
// =======================
var app = builder.Build();

// =======================
// MIDDLEWARE PIPELINE
// =======================

// 1. Custom logging (must be FIRST)
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. Exception handler (lab requirement)
app.UseExceptionHandler("/error");

// 3. Routing
app.UseRouting();

// 4. AuthN
app.UseAuthentication();

// 5. AuthZ
app.UseAuthorization();

// =======================
// ENDPOINTS
// =======================

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

app.Run();