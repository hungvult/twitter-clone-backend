using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.Entities;
using TwitterClone.Api.Services;
using Xunit;

namespace TwitterClone.Api.Tests.Services;

public class BookmarkServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly BookmarkService _bookmarkService;

    public BookmarkServiceTests()
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

        _bookmarkService = new BookmarkService(_context, _mapper);
    }

    [Fact]
    public async Task BookmarkTweetAsync_WhenNotBookmarked_CreatesBookmark()
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
        var result = await _bookmarkService.BookmarkTweetAsync("user1", "tweet1");

        // Assert
        result.Should().BeTrue();

        var bookmark = await _context.Bookmarks.FirstOrDefaultAsync(b => b.UserId == "user1" && b.TweetId == "tweet1");
        bookmark.Should().NotBeNull();
    }

    [Fact]
    public async Task BookmarkTweetAsync_WhenAlreadyBookmarked_ReturnsFalse()
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
        var existingBookmark = new Bookmark
        {
            Id = "bookmark1",
            UserId = "user1",
            TweetId = "tweet1",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddAsync(tweet);
        await _context.Bookmarks.AddAsync(existingBookmark);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bookmarkService.BookmarkTweetAsync("user1", "tweet1");

        // Assert
        result.Should().BeFalse();
        
        var bookmarksCount = await _context.Bookmarks.CountAsync(b => b.UserId == "user1" && b.TweetId == "tweet1");
        bookmarksCount.Should().Be(1); // Should not create duplicate
    }

    [Fact]
    public async Task RemoveBookmarkAsync_WhenBookmarkExists_RemovesBookmark()
    {
        // Arrange
        var bookmark = new Bookmark
        {
            Id = "bookmark1",
            UserId = "user1",
            TweetId = "tweet1",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Bookmarks.AddAsync(bookmark);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bookmarkService.RemoveBookmarkAsync("user1", "tweet1");

        // Assert
        result.Should().BeTrue();
        var bookmarkExists = await _context.Bookmarks.AnyAsync(b => b.Id == "bookmark1");
        bookmarkExists.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveBookmarkAsync_WhenBookmarkNotExists_ReturnsFalse()
    {
        // Act
        var result = await _bookmarkService.RemoveBookmarkAsync("user1", "nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserBookmarksAsync_ReturnsUserBookmarks()
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
        var tweet1 = new Tweet
        {
            Id = "tweet1",
            Text = "Tweet 1",
            CreatedBy = "user2",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        var tweet2 = new Tweet
        {
            Id = "tweet2",
            Text = "Tweet 2",
            CreatedBy = "user2",
            UserLikes = "[]",
            UserRetweets = "[]"
        };
        var bookmark1 = new Bookmark
        {
            Id = "bookmark1",
            UserId = "user1",
            TweetId = "tweet1",
            CreatedAt = DateTime.UtcNow
        };
        var bookmark2 = new Bookmark
        {
            Id = "bookmark2",
            UserId = "user1",
            TweetId = "tweet2",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Users.AddAsync(user);
        await _context.Tweets.AddRangeAsync(tweet1, tweet2);
        await _context.Bookmarks.AddRangeAsync(bookmark1, bookmark2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bookmarkService.GetUserBookmarksAsync("user1", limit: 10);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(b => b.UserId == "user1");
    }

    [Fact]
    public async Task ClearAllBookmarksAsync_RemovesAllUserBookmarks()
    {
        // Arrange
        var bookmark1 = new Bookmark
        {
            Id = "bookmark1",
            UserId = "user1",
            TweetId = "tweet1",
            CreatedAt = DateTime.UtcNow
        };
        var bookmark2 = new Bookmark
        {
            Id = "bookmark2",
            UserId = "user1",
            TweetId = "tweet2",
            CreatedAt = DateTime.UtcNow
        };
        var bookmark3 = new Bookmark
        {
            Id = "bookmark3",
            UserId = "user2",
            TweetId = "tweet1",
            CreatedAt = DateTime.UtcNow
        };
        await _context.Bookmarks.AddRangeAsync(bookmark1, bookmark2, bookmark3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _bookmarkService.ClearAllBookmarksAsync("user1");

        // Assert
        result.Should().BeTrue();
        var user1Bookmarks = await _context.Bookmarks.CountAsync(b => b.UserId == "user1");
        user1Bookmarks.Should().Be(0);
        
        // Other user's bookmarks should remain
        var user2Bookmarks = await _context.Bookmarks.CountAsync(b => b.UserId == "user2");
        user2Bookmarks.Should().Be(1);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

