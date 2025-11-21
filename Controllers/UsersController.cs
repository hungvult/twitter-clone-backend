using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Services;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var users = await _userService.GetAllUsersAsync(limit, offset, currentUserId);
            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch users",
                Details = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
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
                Code = "FETCH_ERROR",
                Message = "Failed to fetch user",
                Details = ex.Message
            });
        }
    }

    [HttpGet("username/{username}")]
    public async Task<ActionResult<UserDto>> GetUserByUsername(string username)
    {
        try
        {
            var user = await _userService.GetUserByUsernameAsync(username);
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
                Code = "FETCH_ERROR",
                Message = "Failed to fetch user",
                Details = ex.Message
            });
        }
    }

    [HttpGet("check-username/{username}")]
    public async Task<ActionResult<bool>> CheckUsernameAvailability(string username)
    {
        try
        {
            var available = await _userService.CheckUsernameAvailabilityAsync(username);
            return Ok(new { available });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "CHECK_ERROR",
                Message = "Failed to check username availability",
                Details = ex.Message
            });
        }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<UserDto>> UpdateUserProfile(string id, [FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var user = await _userService.UpdateUserProfileAsync(id, request);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UPDATE_ERROR",
                Message = "Failed to update profile",
                Details = ex.Message
            });
        }
    }

    [HttpPatch("{id}/username")]
    public async Task<ActionResult<UserDto>> UpdateUsername(string id, [FromBody] UpdateUsernameRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var user = await _userService.UpdateUsernameAsync(id, request.Username);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = "User not found"
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_USERNAME",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UPDATE_ERROR",
                Message = "Failed to update username",
                Details = ex.Message
            });
        }
    }

    [HttpPatch("{id}/theme")]
    public async Task<ActionResult<UserDto>> UpdateTheme(string id, [FromBody] UpdateThemeRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var user = await _userService.UpdateThemeAsync(id, request);
            return Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UPDATE_ERROR",
                Message = "Failed to update theme",
                Details = ex.Message
            });
        }
    }

    [HttpGet("{id}/followers")]
    public async Task<ActionResult<List<UserDto>>> GetFollowers(string id)
    {
        try
        {
            var followers = await _userService.GetFollowersAsync(id);
            return Ok(followers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch followers",
                Details = ex.Message
            });
        }
    }

    [HttpGet("{id}/following")]
    public async Task<ActionResult<List<UserDto>>> GetFollowing(string id)
    {
        try
        {
            var following = await _userService.GetFollowingAsync(id);
            return Ok(following);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch following",
                Details = ex.Message
            });
        }
    }

    [HttpPost("{id}/follow")]
    public async Task<ActionResult> FollowUser(string id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _userService.FollowUserAsync(currentUserId, id);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "FOLLOW_ERROR",
                    Message = "Failed to follow user"
                });

            return Ok(new { message = "User followed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FOLLOW_ERROR",
                Message = "Failed to follow user",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("{id}/follow/{targetUserId}")]
    public async Task<ActionResult> UnfollowUser(string id, string targetUserId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var success = await _userService.UnfollowUserAsync(currentUserId, targetUserId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "UNFOLLOW_ERROR",
                    Message = "Failed to unfollow user"
                });

            return Ok(new { message = "User unfollowed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UNFOLLOW_ERROR",
                Message = "Failed to unfollow user",
                Details = ex.Message
            });
        }
    }

    [HttpPost("{id}/pin-tweet")]
    public async Task<ActionResult> PinTweet(string id, [FromBody] string tweetId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var success = await _userService.PinTweetAsync(id, tweetId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "PIN_ERROR",
                    Message = "Failed to pin tweet"
                });

            return Ok(new { message = "Tweet pinned successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "PIN_ERROR",
                Message = "Failed to pin tweet",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("{id}/pin-tweet")]
    public async Task<ActionResult> UnpinTweet(string id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != id)
                return Forbid();

            var success = await _userService.UnpinTweetAsync(id);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "UNPIN_ERROR",
                    Message = "Failed to unpin tweet"
                });

            return Ok(new { message = "Tweet unpinned successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UNPIN_ERROR",
                Message = "Failed to unpin tweet",
                Details = ex.Message
            });
        }
    }
}
