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
    [HttpPost("signout")]
    public new IActionResult SignOut()
    {
        // In a production app, you would invalidate the refresh token here
        return Ok(new { message = "Signed out successfully" });
    }
}
