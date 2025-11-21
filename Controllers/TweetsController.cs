using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Services;

namespace TwitterClone.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TweetsController : ControllerBase
{
    private readonly ITweetService _tweetService;

    public TweetsController(ITweetService tweetService)
    {
        _tweetService = tweetService;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException();
    }

    private bool IsAdmin()
    {
        return User.FindFirst("IsAdmin")?.Value == "True";
    }

    [HttpGet]
    public async Task<ActionResult<List<TweetDto>>> GetTimeline(
        [FromQuery] int limit = 20,
        [FromQuery] DateTime? before = null)
    {
        try
        {
            var tweets = await _tweetService.GetTimelineTweetsAsync(limit, before);
            return Ok(tweets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch timeline",
                Details = ex.Message
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TweetDto>> GetTweetById(string id)
    {
        try
        {
            var tweet = await _tweetService.GetTweetByIdAsync(id);
            if (tweet == null)
                return NotFound(new ErrorResponse
                {
                    Code = "TWEET_NOT_FOUND",
                    Message = "Tweet not found"
                });

            return Ok(tweet);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch tweet",
                Details = ex.Message
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TweetDto>> CreateTweet([FromBody] CreateTweetRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tweet = await _tweetService.CreateTweetAsync(userId, request);
            return CreatedAtAction(nameof(GetTweetById), new { id = tweet.Id }, tweet);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Code = "INVALID_TWEET",
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "CREATE_ERROR",
                Message = "Failed to create tweet",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTweet(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = IsAdmin();
            
            var success = await _tweetService.DeleteTweetAsync(id, userId, isAdmin);
            
            if (!success)
                return NotFound(new ErrorResponse
                {
                    Code = "TWEET_NOT_FOUND",
                    Message = "Tweet not found or unauthorized"
                });

            return Ok(new { message = "Tweet deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "DELETE_ERROR",
                Message = "Failed to delete tweet",
                Details = ex.Message
            });
        }
    }

    [HttpGet("{id}/replies")]
    public async Task<ActionResult<List<TweetDto>>> GetTweetReplies(
        string id,
        [FromQuery] int limit = 20)
    {
        try
        {
            var replies = await _tweetService.GetTweetRepliesAsync(id, limit);
            return Ok(replies);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch replies",
                Details = ex.Message
            });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<List<TweetDto>>> GetUserTweets(
        string userId,
        [FromQuery] int limit = 20,
        [FromQuery] bool includeReplies = false)
    {
        try
        {
            var tweets = await _tweetService.GetUserTweetsAsync(userId, limit, includeReplies);
            return Ok(tweets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch user tweets",
                Details = ex.Message
            });
        }
    }

    [HttpGet("user/{userId}/media")]
    public async Task<ActionResult<List<TweetDto>>> GetUserMediaTweets(
        string userId,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tweets = await _tweetService.GetUserMediaTweetsAsync(userId, limit);
            return Ok(tweets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch user media tweets",
                Details = ex.Message
            });
        }
    }

    [HttpGet("user/{userId}/likes")]
    public async Task<ActionResult<List<TweetDto>>> GetUserLikedTweets(
        string userId,
        [FromQuery] int limit = 20)
    {
        try
        {
            var tweets = await _tweetService.GetUserLikedTweetsAsync(userId, limit);
            return Ok(tweets);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "FETCH_ERROR",
                Message = "Failed to fetch liked tweets",
                Details = ex.Message
            });
        }
    }

    [HttpPost("{id}/like")]
    public async Task<ActionResult> LikeTweet(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _tweetService.LikeTweetAsync(id, userId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "LIKE_ERROR",
                    Message = "Failed to like tweet"
                });

            return Ok(new { message = "Tweet liked successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "LIKE_ERROR",
                Message = "Failed to like tweet",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("{id}/like")]
    public async Task<ActionResult> UnlikeTweet(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _tweetService.UnlikeTweetAsync(id, userId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "UNLIKE_ERROR",
                    Message = "Failed to unlike tweet"
                });

            return Ok(new { message = "Tweet unliked successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UNLIKE_ERROR",
                Message = "Failed to unlike tweet",
                Details = ex.Message
            });
        }
    }

    [HttpPost("{id}/retweet")]
    public async Task<ActionResult> Retweet(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _tweetService.RetweetAsync(id, userId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "RETWEET_ERROR",
                    Message = "Failed to retweet"
                });

            return Ok(new { message = "Retweeted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "RETWEET_ERROR",
                Message = "Failed to retweet",
                Details = ex.Message
            });
        }
    }

    [HttpDelete("{id}/retweet")]
    public async Task<ActionResult> Unretweet(string id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var success = await _tweetService.UnretweetAsync(id, userId);
            
            if (!success)
                return BadRequest(new ErrorResponse
                {
                    Code = "UNRETWEET_ERROR",
                    Message = "Failed to unretweet"
                });

            return Ok(new { message = "Unretweeted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ErrorResponse
            {
                Code = "UNRETWEET_ERROR",
                Message = "Failed to unretweet",
                Details = ex.Message
            });
        }
    }
}
