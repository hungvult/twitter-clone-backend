using System.Text.Json;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;
using TwitterClone.Api.Services;
using Xunit;

namespace TwitterClone.Api.Tests.Services;

public class TweetServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly TweetService _tweetService;

    public TweetServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var config = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TwitterClone.Api.Mappings.MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _tweetService = new TweetService(_context, _mapper);
    }

    [Fact]
    public async Task GetTweetByIdAsync_WhenTweetExists_ReturnsTweetDto()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.GetTweetByIdAsync("tweet1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("tweet1");
        result.Text.Should().Be("Test tweet");
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task GetTweetByIdAsync_WhenTweetNotExists_ReturnsNull()
    {
        // Act
        var result = await _tweetService.GetTweetByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTweetAsync_WithValidData_CreatesTweet()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new CreateTweetRequest
        {
            Text = "New tweet",
            Images = new List<ImageData>
            {
                new ImageData { Id = "img1", Src = "http://example.com/img1.jpg", Alt = "Image 1" }
            }
        };

        // Act
        var result = await _tweetService.CreateTweetAsync("user1", request);

        // Assert
        result.Should().NotBeNull();
        result.Text.Should().Be("New tweet");
        result.Images.Should().HaveCount(1);

        var tweet = await _context.Tweets.FindAsync(result.Id);
        tweet.Should().NotBeNull();
        tweet!.Text.Should().Be("New tweet");
    }

    [Fact]
    public async Task DeleteTweetAsync_WhenUserIsOwner_DeletesTweet()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.DeleteTweetAsync("tweet1", "user1", isAdmin: false);

        // Assert
        result.Should().BeTrue();
        var deletedTweet = await _context.Tweets.FindAsync("tweet1");
        deletedTweet.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTweetAsync_WhenUserIsAdmin_DeletesTweet()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user2", // Different user
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.DeleteTweetAsync("tweet1", "user1", isAdmin: true);

        // Assert
        result.Should().BeTrue();
        var deletedTweet = await _context.Tweets.FindAsync("tweet1");
        deletedTweet.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTweetAsync_WhenUserIsNotOwnerAndNotAdmin_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user2", // Different user
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.DeleteTweetAsync("tweet1", "user1", isAdmin: false);

        // Assert
        result.Should().BeFalse();
        var tweetStillExists = await _context.Tweets.FindAsync("tweet1");
        tweetStillExists.Should().NotBeNull();
    }

    [Fact]
    public async Task LikeTweetAsync_WhenNotLiked_AddsLike()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user2",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.LikeTweetAsync("tweet1", "user1");

        // Assert
        result.Should().BeTrue();
        var updatedTweet = await _context.Tweets.FindAsync("tweet1");
        var likes = JsonSerializer.Deserialize<List<string>>(updatedTweet!.UserLikes);
        likes.Should().Contain("user1");
    }

    [Fact]
    public async Task UnlikeTweetAsync_WhenLiked_RemovesLike()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user2",
            UserLikes = JsonSerializer.Serialize(new List<string> { "user1" }),
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.UnlikeTweetAsync("tweet1", "user1");

        // Assert
        result.Should().BeTrue();
        var updatedTweet = await _context.Tweets.FindAsync("tweet1");
        var likes = JsonSerializer.Deserialize<List<string>>(updatedTweet!.UserLikes);
        likes.Should().NotContain("user1");
    }

    [Fact]
    public async Task RetweetAsync_WhenNotRetweeted_AddsRetweet()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var tweet = new Tweet
        {
            Id = "tweet1",
            Text = "Test tweet",
            CreatedBy = "user2",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.RetweetAsync("tweet1", "user1");

        // Assert
        result.Should().BeTrue();
        var updatedTweet = await _context.Tweets.FindAsync("tweet1");
        var retweets = JsonSerializer.Deserialize<List<string>>(updatedTweet!.UserRetweets);
        retweets.Should().Contain("user1");
    }

    [Fact]
    public async Task GetTimelineTweetsAsync_WithLimit_ReturnsLimitedTweets()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);

        for (int i = 1; i <= 10; i++)
        {
            await _context.Tweets.AddAsync(new Tweet
            {
                Id = $"tweet{i}",
                Text = $"Tweet {i}",
                CreatedBy = "user1",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                UserLikes = "[]",
                UserRetweets = "[]"
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.GetTimelineTweetsAsync(limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetTimelineTweetsAsync_ExcludesReplies()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);

        var mainTweet = new Tweet
        {
            Id = "tweet1",
            Text = "Main tweet",
            CreatedBy = "user1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        var reply = new Tweet
        {
            Id = "reply1",
            Text = "Reply",
            CreatedBy = "user1",
            ParentId = "tweet1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Tweets.AddRangeAsync(mainTweet, reply);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.GetTimelineTweetsAsync(limit: 10);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("tweet1");
        result.Should().NotContain(t => t.Id == "reply1");
    }

    [Fact]
    public async Task GetTweetRepliesAsync_ReturnsOnlyReplies()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test User",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);

        var mainTweet = new Tweet
        {
            Id = "tweet1",
            Text = "Main tweet",
            CreatedBy = "user1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        var reply1 = new Tweet
        {
            Id = "reply1",
            Text = "Reply 1",
            CreatedBy = "user1",
            ParentId = "tweet1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        var reply2 = new Tweet
        {
            Id = "reply2",
            Text = "Reply 2",
            CreatedBy = "user1",
            ParentId = "tweet1",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        await _context.Tweets.AddRangeAsync(mainTweet, reply1, reply2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _tweetService.GetTweetRepliesAsync("tweet1", limit: 10);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.Parent?.Id == "tweet1");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

