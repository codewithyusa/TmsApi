using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Authorization;

public sealed class CourseInstructorHandler
    : AuthorizationHandler<CourseInstructorRequirement, Course>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CourseInstructorRequirement requirement,
        Course resource)
    {
        // Admins can manage any course.
        if (context.User.IsInRole("Admin"))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // User must be an Instructor.
        if (!context.User.IsInRole("Instructor"))
        {
            return Task.CompletedTask;
        }

        // Get the authenticated user's ID from the JWT.
        var userId = context.User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        // Instructor can only manage courses they own/teach.
        if (!string.IsNullOrWhiteSpace(userId) &&
            resource.InstructorId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
