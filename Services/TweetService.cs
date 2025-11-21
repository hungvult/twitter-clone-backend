using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Services;

public interface ITweetService
{
    Task<TweetDto?> GetTweetByIdAsync(string id);
    Task<List<TweetDto>> GetTimelineTweetsAsync(int limit, DateTime? before = null);
    Task<List<TweetDto>> GetTweetRepliesAsync(string tweetId, int limit);
    Task<List<TweetDto>> GetUserTweetsAsync(string userId, int limit, bool includeReplies = false);
    Task<List<TweetDto>> GetUserMediaTweetsAsync(string userId, int limit);
    Task<List<TweetDto>> GetUserLikedTweetsAsync(string userId, int limit);
    Task<TweetDto> CreateTweetAsync(string userId, CreateTweetRequest request);
    Task<bool> DeleteTweetAsync(string tweetId, string userId, bool isAdmin = false);
    Task<bool> LikeTweetAsync(string tweetId, string userId);
    Task<bool> UnlikeTweetAsync(string tweetId, string userId);
    Task<bool> RetweetAsync(string tweetId, string userId);
    Task<bool> UnretweetAsync(string tweetId, string userId);
}

public class TweetService : ITweetService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public TweetService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TweetDto?> GetTweetByIdAsync(string id)
    {
        var tweet = await _context.Tweets
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        return tweet == null ? null : _mapper.Map<TweetDto>(tweet);
    }

    public async Task<List<TweetDto>> GetTimelineTweetsAsync(int limit, DateTime? before = null)
    {
        var query = _context.Tweets
            .Include(t => t.User)
            .Where(t => t.ParentId == null); // Not replies

        if (before.HasValue)
            query = query.Where(t => t.CreatedAt < before.Value);

        var tweets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<TweetDto>>(tweets);
    }

    public async Task<List<TweetDto>> GetTweetRepliesAsync(string tweetId, int limit)
    {
        var tweets = await _context.Tweets
            .Include(t => t.User)
            .Where(t => t.ParentId == tweetId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<TweetDto>>(tweets);
    }

    public async Task<List<TweetDto>> GetUserTweetsAsync(string userId, int limit, bool includeReplies = false)
    {
        var query = _context.Tweets
            .Include(t => t.User)
            .Where(t => t.CreatedBy == userId);

        if (!includeReplies)
            query = query.Where(t => t.ParentId == null);

        var tweets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<TweetDto>>(tweets);
    }

    public async Task<List<TweetDto>> GetUserMediaTweetsAsync(string userId, int limit)
    {
        var tweets = await _context.Tweets
            .Include(t => t.User)
            .Where(t => t.CreatedBy == userId && t.Images != null)
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<TweetDto>>(tweets);
    }

    public async Task<List<TweetDto>> GetUserLikedTweetsAsync(string userId, int limit)
    {
        // SQL Server JSON query using LIKE for array contains
        var tweets = await _context.Tweets
            .Include(t => t.User)
            .Where(t => t.UserLikes.Contains($"\"{userId}\""))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<TweetDto>>(tweets);
    }

    public async Task<TweetDto> CreateTweetAsync(string userId, CreateTweetRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Validate
        if (string.IsNullOrEmpty(request.Text) && (request.Images == null || request.Images.Count == 0))
            throw new ArgumentException("Tweet must have text or images");

        var isAdmin = user.Username == "ccrsxx";
        var maxLength = isAdmin ? 560 : 280;

        if (!string.IsNullOrEmpty(request.Text) && request.Text.Length > maxLength)
            throw new ArgumentException($"Tweet text cannot exceed {maxLength} characters");

        var tweet = new Tweet
        {
            Id = Guid.NewGuid().ToString(),
            Text = request.Text,
            Images = request.Images != null ? JsonSerializer.Serialize(request.Images) : null,
            ParentId = request.Parent?.Id,
            ParentUsername = request.Parent?.Username,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tweets.Add(tweet);

        // Update user stats
        user.TotalTweets++;
        if (request.Images != null && request.Images.Count > 0)
            user.TotalPhotos += request.Images.Count;

        // If reply, increment parent's reply count
        if (!string.IsNullOrEmpty(request.Parent?.Id))
        {
            var parentTweet = await _context.Tweets.FindAsync(request.Parent.Id);
            if (parentTweet != null)
                parentTweet.UserReplies++;
        }

        await _context.SaveChangesAsync();

        // Load user for response
        tweet.User = user;
        return _mapper.Map<TweetDto>(tweet);
    }

    public async Task<bool> DeleteTweetAsync(string tweetId, string userId, bool isAdmin = false)
    {
        var tweet = await _context.Tweets.Include(t => t.User).FirstOrDefaultAsync(t => t.Id == tweetId);
        if (tweet == null)
            return false;

        // Check permission
        if (tweet.CreatedBy != userId && !isAdmin)
            return false;

        // Decrement user stats
        tweet.User.TotalTweets--;
        if (!string.IsNullOrEmpty(tweet.Images))
        {
            var images = JsonSerializer.Deserialize<List<ImageData>>(tweet.Images);
            if (images != null)
                tweet.User.TotalPhotos -= images.Count;
        }

        // If reply, decrement parent's reply count
        if (!string.IsNullOrEmpty(tweet.ParentId))
        {
            var parentTweet = await _context.Tweets.FindAsync(tweet.ParentId);
            if (parentTweet != null && parentTweet.UserReplies > 0)
                parentTweet.UserReplies--;
        }

        // Remove from bookmarks
        var bookmarks = await _context.Bookmarks.Where(b => b.TweetId == tweetId).ToListAsync();
        _context.Bookmarks.RemoveRange(bookmarks);

        // Remove from user stats (likes and retweets)
        var allUserStats = await _context.UserStats.ToListAsync();
        foreach (var stats in allUserStats)
        {
            var likes = JsonSerializer.Deserialize<List<string>>(stats.Likes) ?? new List<string>();
            var tweets = JsonSerializer.Deserialize<List<string>>(stats.Tweets) ?? new List<string>();

            var modified = false;
            if (likes.Remove(tweetId))
                modified = true;
            if (tweets.Remove(tweetId))
                modified = true;

            if (modified)
            {
                stats.Likes = JsonSerializer.Serialize(likes);
                stats.Tweets = JsonSerializer.Serialize(tweets);
                stats.UpdatedAt = DateTime.UtcNow;
            }
        }

        _context.Tweets.Remove(tweet);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> LikeTweetAsync(string tweetId, string userId)
    {
        var tweet = await _context.Tweets.FindAsync(tweetId);
        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);

        if (tweet == null || userStats == null)
            return false;

        var likes = JsonSerializer.Deserialize<List<string>>(tweet.UserLikes) ?? new List<string>();
        var userLikes = JsonSerializer.Deserialize<List<string>>(userStats.Likes) ?? new List<string>();

        if (!likes.Contains(userId))
            likes.Add(userId);

        if (!userLikes.Contains(tweetId))
            userLikes.Add(tweetId);

        tweet.UserLikes = JsonSerializer.Serialize(likes);
        tweet.UpdatedAt = DateTime.UtcNow;

        userStats.Likes = JsonSerializer.Serialize(userLikes);
        userStats.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnlikeTweetAsync(string tweetId, string userId)
    {
        var tweet = await _context.Tweets.FindAsync(tweetId);
        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);

        if (tweet == null || userStats == null)
            return false;

        var likes = JsonSerializer.Deserialize<List<string>>(tweet.UserLikes) ?? new List<string>();
        var userLikes = JsonSerializer.Deserialize<List<string>>(userStats.Likes) ?? new List<string>();

        likes.Remove(userId);
        userLikes.Remove(tweetId);

        tweet.UserLikes = JsonSerializer.Serialize(likes);
        tweet.UpdatedAt = DateTime.UtcNow;

        userStats.Likes = JsonSerializer.Serialize(userLikes);
        userStats.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RetweetAsync(string tweetId, string userId)
    {
        var tweet = await _context.Tweets.FindAsync(tweetId);
        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);

        if (tweet == null || userStats == null)
            return false;

        var retweets = JsonSerializer.Deserialize<List<string>>(tweet.UserRetweets) ?? new List<string>();
        var userRetweets = JsonSerializer.Deserialize<List<string>>(userStats.Tweets) ?? new List<string>();

        if (!retweets.Contains(userId))
            retweets.Add(userId);

        if (!userRetweets.Contains(tweetId))
            userRetweets.Add(tweetId);

        tweet.UserRetweets = JsonSerializer.Serialize(retweets);
        tweet.UpdatedAt = DateTime.UtcNow;

        userStats.Tweets = JsonSerializer.Serialize(userRetweets);
        userStats.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnretweetAsync(string tweetId, string userId)
    {
        var tweet = await _context.Tweets.FindAsync(tweetId);
        var userStats = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);

        if (tweet == null || userStats == null)
            return false;

        var retweets = JsonSerializer.Deserialize<List<string>>(tweet.UserRetweets) ?? new List<string>();
        var userRetweets = JsonSerializer.Deserialize<List<string>>(userStats.Tweets) ?? new List<string>();

        retweets.Remove(userId);
        userRetweets.Remove(tweetId);

        tweet.UserRetweets = JsonSerializer.Serialize(retweets);
        tweet.UpdatedAt = DateTime.UtcNow;

        userStats.Tweets = JsonSerializer.Serialize(userRetweets);
        userStats.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
