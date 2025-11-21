using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Services;

public interface IBookmarkService
{
    Task<List<BookmarkDto>> GetUserBookmarksAsync(string userId);
    Task<bool> BookmarkTweetAsync(string userId, string tweetId);
    Task<bool> RemoveBookmarkAsync(string userId, string tweetId);
    Task<bool> ClearAllBookmarksAsync(string userId);
}

public class BookmarkService : IBookmarkService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public BookmarkService(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<BookmarkDto>> GetUserBookmarksAsync(string userId)
    {
        var bookmarks = await _context.Bookmarks
            .Include(b => b.Tweet)
                .ThenInclude(t => t.User)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var bookmarkDtos = _mapper.Map<List<BookmarkDto>>(bookmarks);
        
        // Map tweets
        for (int i = 0; i < bookmarks.Count; i++)
        {
            bookmarkDtos[i].Tweet = _mapper.Map<TweetDto>(bookmarks[i].Tweet);
        }

        return bookmarkDtos;
    }

    public async Task<bool> BookmarkTweetAsync(string userId, string tweetId)
    {
        // Check if already bookmarked
        var exists = await _context.Bookmarks
            .AnyAsync(b => b.UserId == userId && b.TweetId == tweetId);

        if (exists)
            return false;

        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            TweetId = tweetId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bookmarks.Add(bookmark);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveBookmarkAsync(string userId, string tweetId)
    {
        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.TweetId == tweetId);

        if (bookmark == null)
            return false;

        _context.Bookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ClearAllBookmarksAsync(string userId)
    {
        var bookmarks = await _context.Bookmarks
            .Where(b => b.UserId == userId)
            .ToListAsync();

        _context.Bookmarks.RemoveRange(bookmarks);
        await _context.SaveChangesAsync();

        return true;
    }
}
