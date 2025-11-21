using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Services;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookmarksController : ControllerBase
{
    private readonly IBookmarkService _bookmarkService;

    public BookmarksController(IBookmarkService bookmarkService)
    {
        _bookmarkService = bookmarkService;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<BookmarkDto>>> GetUserBookmarks(string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
                return Forbid();

            var bookmarks = await _bookmarkService.GetUserBookmarksAsync(userId);
            return Ok(bookmarks);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch bookmarks",
                Details = ex.Message
            });
        }
    }

    [HttpPost("tweet/{tweetId}")]
    public async Task<ActionResult> BookmarkTweet(string tweetId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _bookmarkService.BookmarkTweetAsync(userId, tweetId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "BOOKMARK_ERROR",
                    Message = "Tweet already bookmarked"
                });

            return Ok(new { message = "Tweet bookmarked successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "BOOKMARK_ERROR",
                Message = "Failed to bookmark tweet",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("tweet/{tweetId}")]
    public async Task<ActionResult> RemoveBookmark(string tweetId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _bookmarkService.RemoveBookmarkAsync(userId, tweetId);
            
            if (!success)
                return NotFound(new ErrorResponse
                {
                    Code = "BOOKMARK_NOT_FOUND",
                    Message = "Bookmark not found"
                });

            return Ok(new { message = "Bookmark removed successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "REMOVE_ERROR",
                Message = "Failed to remove bookmark",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("user/{userId}")]
    public async Task<ActionResult> ClearAllBookmarks(string userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
                return Forbid();

            var success = await _bookmarkService.ClearAllBookmarksAsync(userId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "CLEAR_ERROR",
                    Message = "Failed to clear bookmarks"
                });

            return Ok(new { message = "All bookmarks cleared successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "CLEAR_ERROR",
                Message = "Failed to clear bookmarks",
                Details = ex.Message
            });
        }
    }
}
