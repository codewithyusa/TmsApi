using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// SERVICES
builder.Services.AddAuthentication("Training")
    .AddScheme<AuthenticationSchemeOptions, TrainingAuthHandler>("Training", null);

builder.Services.AddAuthorization();

var app = builder.Build();

// PIPELINE ORDER (IMPORTANT)

// 1. Logging FIRST (outer wrapper)
app.UseMiddleware<RequestLoggingMiddleware>();

// 2. Exception handler (optional but required by lab design)
app.UseExceptionHandler("/error");

// 3. Routing
app.UseRouting();

// 4. Authentication
app.UseAuthentication();

// 5. Authorization
app.UseAuthorization();

// 6. Endpoint LAST
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