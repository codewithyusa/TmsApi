using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Services;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/{version:apiVersion}/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<TmsUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TmsDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(
        UserManager<TmsUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TmsDbContext context,
        TokenService tokenService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
    }

    // ============================================================
    // REGISTER
    // ============================================================

    public record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string Role);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var existingUser =
            await _userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            // Prevent account enumeration.
            return Ok(new
            {
                message = "Registration request received."
            });
        }

        var user = new TmsUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        var result =
            await _userManager.CreateAsync(
                user,
                request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .Select(e => e.Description);

            return BadRequest(new
            {
                errors
            });
        }

        // Ensure requested role exists.
        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            var roleResult =
                await _roleManager.CreateAsync(
                    new IdentityRole(request.Role));

            if (!roleResult.Succeeded)
            {
                // Clean up the user if role creation fails.
                await _userManager.DeleteAsync(user);

                return BadRequest(new
                {
                    errors = roleResult.Errors
                        .Select(e => e.Description)
                });
            }
        }

        var addRoleResult =
            await _userManager.AddToRoleAsync(
                user,
                request.Role);

        if (!addRoleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                errors = addRoleResult.Errors
                    .Select(e => e.Description)
            });
        }

        return Ok(new
        {
            message = "Registration successful."
        });
    }

    // ============================================================
    // LOGIN
    // ============================================================

    public record LoginRequest(
        string Email,
        string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var user =
            await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        // Check lockout status.
        if (await _userManager.IsLockedOutAsync(user))
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                new
                {
                    detail =
                        "Account locked due to multiple failed login attempts. Try again in 15 minutes."
                });
        }

        // Validate password.
        var validPassword =
            await _userManager.CheckPasswordAsync(
                user,
                request.Password);

        if (!validPassword)
        {
            await _userManager.AccessFailedAsync(user);

            return Unauthorized(new
            {
                detail = "Invalid credentials."
            });
        }

        // Successful login resets failed login attempts.
        await _userManager.ResetAccessFailedCountAsync(user);

        // Get user's roles.
        var roles =
            await _userManager.GetRolesAsync(user);

        // Generate JWT access token.
        var accessToken =
            _tokenService.GenerateJwt(
                user,
                roles);

        // Generate initial refresh token.
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token
        });
    }

    // ============================================================
    // REFRESH TOKEN
    // ============================================================

    public record RefreshRequest(
        string RefreshToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Unauthorized(new
            {
                detail = "Refresh token is required."
            });
        }

        // Find refresh token.
        var storedToken =
            await _context.RefreshTokens
                .FirstOrDefaultAsync(
                    rt => rt.Token == request.RefreshToken);

        if (storedToken == null)
        {
            return Unauthorized(new
            {
                detail = "Invalid refresh token."
            });
        }

        // ========================================================
        // TOKEN THEFT DETECTION
        // ========================================================
        //
        // A refresh token can only be used once.
        //
        // If an already-used token is submitted again,
        // assume it was stolen and revoke every refresh
        // token belonging to this user.
        //

        if (storedToken.IsUsed)
        {
            var userTokens =
                await _context.RefreshTokens
                    .Where(rt =>
                        rt.UserId == storedToken.UserId)
                    .ToListAsync();

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync();

            return Unauthorized(new
            {
                detail =
                    "Token theft detected. All user sessions revoked."
            });
        }

        // ========================================================
        // CHECK REVOCATION
        // ========================================================

        if (storedToken.IsRevoked)
        {
            return Unauthorized(new
            {
                detail =
                    "Refresh token has been revoked."
            });
        }

        // ========================================================
        // CHECK EXPIRATION
        // ========================================================

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return Unauthorized(new
            {
                detail =
                    "Refresh token has expired."
            });
        }

        // ========================================================
        // FIND USER
        // ========================================================

        var user =
            await _userManager.FindByIdAsync(
                storedToken.UserId);

        if (user == null)
        {
            storedToken.IsRevoked = true;

            await _context.SaveChangesAsync();

            return Unauthorized(new
            {
                detail =
                    "User associated with refresh token was not found."
            });
        }

        // ========================================================
        // ROTATE REFRESH TOKEN
        // ========================================================

        // The old refresh token can never be used again.
        storedToken.IsUsed = true;

        // Create completely new refresh token.
        var newRefreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(newRefreshToken);

        // ========================================================
        // GENERATE NEW ACCESS TOKEN

        var roles =
            await _userManager.GetRolesAsync(user);

        var newAccessToken =
            _tokenService.GenerateJwt(
                user,
                roles);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }
}
