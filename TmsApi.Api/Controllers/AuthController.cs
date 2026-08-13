using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Auth;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login(
        [FromBody] LoginRequest request,
        [FromServices] IWebHostEnvironment env)
    {
        // Demo account for Module 10 transport testing
        if (request.Username == "admin" &&
            request.Password == "Password123!")
        {
            var dummyJwt = "header.payload.signature-demo-token";

            // HttpOnly authentication cookie.
            // JavaScript cannot read this cookie.
            Response.Cookies.Append(
                "tms_auth",
                dummyJwt,
                new CookieOptions
                {
                    HttpOnly = true,

                    // HTTPS required in production,
                    // HTTP permitted during local development.
                    Secure = !env.IsDevelopment(),

                    SameSite = SameSiteMode.Strict,

                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                });

            return Ok(
                new UserProfileDto(
                    "System Admin",
                    "Admin"));
        }

        return Unauthorized(
            new
            {
                detail = "Invalid username or password."
            });
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Browser automatically attaches the authentication cookie.
        if (Request.Cookies.TryGetValue("tms_auth", out _))
        {
            return Ok(
                new UserProfileDto(
                    "System Admin",
                    "Admin"));
        }

        return Unauthorized(
            new
            {
                detail =
                    "Session expired or missing authentication cookie."
            });
    }
}