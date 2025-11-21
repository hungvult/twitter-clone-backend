using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Services;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleAuth([FromBody] GoogleAuthRequest request)
    {
        try
        {
            var response = await _authService.AuthenticateWithGoogleAsync(request.IdToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "INVALID_TOKEN",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "AUTH_ERROR",
                Message = "Authentication failed",
                Details = ex.Message
            });
        }
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] GoogleAuthRequest request)
    {
        // Register endpoint is the same as Google auth (creates user if not exists)
        try
        {
            var response = await _authService.AuthenticateWithGoogleAsync(request.IdToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new ErrorResponse
            {
                Code = "INVALID_TOKEN",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "AUTH_ERROR",
                Message = "Authentication failed",
                Details = ex.Message
            });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] string refreshToken)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(refreshToken);
            if (response == null)
                return Unauthorized(new ErrorResponse
                {
                    Code = "INVALID_REFRESH_TOKEN",
                    Message = "Invalid or expired refresh token"
                });

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "REFRESH_ERROR",
                Message = "Token refresh failed",
                Details = ex.Message
            });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new ErrorResponse
                {
                    Code = "INVALID_TOKEN",
                    Message = "User ID not found in token"
                });

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new ErrorResponse
                {
                    Code = "USER_NOT_FOUND",
                    Message = "User not found"
                });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "GET_USER_ERROR",
                Message = "Failed to get current user",
                Details = ex.Message
            });
        }
    }

    [Authorize]
    [HttpPost("signout")]
    public new IActionResult SignOut()
    {
        // In a production app, you would invalidate the refresh token here
        return Ok(new { message = "Signed out successfully" });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Same as signout
        return Ok(new { message = "Logged out successfully" });
    }
}
