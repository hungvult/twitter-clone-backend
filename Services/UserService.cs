using System.Text.Json;
using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Data;
using TwitterClone.Api.Hubs;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(string id);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<bool> CheckUsernameAvailabilityAsync(string username);
    Task<UserDto> UpdateUserProfileAsync(string userId, UpdateUserProfileRequest request);
    Task<UserDto> UpdateUsernameAsync(string userId, string newUsername);
    Task<UserDto> UpdateThemeAsync(string userId, UpdateThemeRequest request);
    Task<List<UserDto>> GetAllUsersAsync(int limit, int offset, string? excludeUserId = null);
    Task<List<UserDto>> GetFollowersAsync(string userId);
    Task<List<UserDto>> GetFollowingAsync(string userId);
    Task<bool> FollowUserAsync(string currentUserId, string targetUserId);
    Task<bool> UnfollowUserAsync(string currentUserId, string targetUserId);
    Task<bool> PinTweetAsync(string userId, string tweetId);
    Task<bool> UnpinTweetAsync(string userId);
}

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IHubContext<UserHub> _userHubContext;

    public UserService(ApplicationDbContext context, IMapper mapper, IHubContext<UserHub> userHubContext)
    {
        _context = context;
        _mapper = mapper;
        _userHubContext = userHubContext;
    }

    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByUsernameAsync(string username)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<bool> CheckUsernameAvailabilityAsync(string username)
    {
        return !await _context.Users.AnyAsync(u => u.Username == username);
    }

    public async Task<UserDto> UpdateUserProfileAsync(string userId, UpdateUserProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        if (!string.IsNullOrEmpty(request.Name))
            user.Name = request.Name;
        
        // Only update fields that are provided (not null)
        // Empty strings are allowed to clear fields
        if (request.Bio != null)
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio;
        
        if (request.Website != null)
            user.Website = string.IsNullOrWhiteSpace(request.Website) ? null : request.Website;
        
        if (request.Location != null)
            user.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location;
        
        if (!string.IsNullOrEmpty(request.PhotoURL))
            user.PhotoURL = request.PhotoURL;
        
        // CoverPhotoURL can be null to remove cover photo
        if (request.CoverPhotoURL != null)
            user.CoverPhotoURL = string.IsNullOrWhiteSpace(request.CoverPhotoURL) ? null : request.CoverPhotoURL;
        
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        
        var userDto = _mapper.Map<UserDto>(user);
        
        // Broadcast user update via SignalR
        try
        {
            await _userHubContext.Clients.Group($"user_{userId}")
                .SendAsync("UserUpdated", userDto);
            Console.WriteLine($"[UserService] Broadcasted profile update for user {userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserService] SignalR broadcast failed: {ex.Message}");
        }
        
        return userDto;
    }

    public async Task<UserDto> UpdateUsernameAsync(string userId, string newUsername)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        // Validate username
        if (newUsername.Length < 4 || newUsername.Length > 15)
            throw new ArgumentException("Username must be between 4-15 characters");

        if (!Regex.IsMatch(newUsername, @"^\w+$"))
            throw new ArgumentException("Username can only contain letters, numbers, and underscores");

        if (!newUsername.Any(char.IsLetter))
            throw new ArgumentException("Username must contain at least one letter");

        if (user.Username == newUsername)
            throw new ArgumentException("New username cannot be the same as current username");

        if (!await CheckUsernameAvailabilityAsync(newUsername))
            throw new ArgumentException("Username is already taken");

        user.Username = newUsername;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateThemeAsync(string userId, UpdateThemeRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new KeyNotFoundException("User not found");

        user.Theme = request.Theme;
        user.Accent = request.Accent;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync(int limit, int offset, string? excludeUserId = null)
    {
        var query = _context.Users.AsQueryable();

        if (!string.IsNullOrEmpty(excludeUserId))
            query = query.Where(u => u.Id != excludeUserId);

        var users = await query
            .OrderBy(u => u.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<UserDto>>(users);
    }

    public async Task<List<UserDto>> GetFollowersAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new List<UserDto>();

        var followerIds = JsonSerializer.Deserialize<List<string>>(user.Followers) ?? new List<string>();
        
        var followers = await _context.Users
            .Where(u => followerIds.Contains(u.Id))
            .ToListAsync();

        return _mapper.Map<List<UserDto>>(followers);
    }

    public async Task<List<UserDto>> GetFollowingAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new List<UserDto>();

        var followingIds = JsonSerializer.Deserialize<List<string>>(user.Following) ?? new List<string>();
        
        var following = await _context.Users
            .Where(u => followingIds.Contains(u.Id))
            .ToListAsync();

        return _mapper.Map<List<UserDto>>(following);
    }

    public async Task<bool> FollowUserAsync(string currentUserId, string targetUserId)
    {
        if (currentUserId == targetUserId)
            return false;

        var currentUser = await _context.Users.FindAsync(currentUserId);
        var targetUser = await _context.Users.FindAsync(targetUserId);

        if (currentUser == null || targetUser == null)
            return false;

        var currentFollowing = JsonSerializer.Deserialize<List<string>>(currentUser.Following) ?? new List<string>();
        var targetFollowers = JsonSerializer.Deserialize<List<string>>(targetUser.Followers) ?? new List<string>();

        if (!currentFollowing.Contains(targetUserId))
            currentFollowing.Add(targetUserId);

        if (!targetFollowers.Contains(currentUserId))
            targetFollowers.Add(currentUserId);

        currentUser.Following = JsonSerializer.Serialize(currentFollowing);
        currentUser.UpdatedAt = DateTime.UtcNow;

        targetUser.Followers = JsonSerializer.Serialize(targetFollowers);
        targetUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnfollowUserAsync(string currentUserId, string targetUserId)
    {
        var currentUser = await _context.Users.FindAsync(currentUserId);
        var targetUser = await _context.Users.FindAsync(targetUserId);

        if (currentUser == null || targetUser == null)
            return false;

        var currentFollowing = JsonSerializer.Deserialize<List<string>>(currentUser.Following) ?? new List<string>();
        var targetFollowers = JsonSerializer.Deserialize<List<string>>(targetUser.Followers) ?? new List<string>();

        currentFollowing.Remove(targetUserId);
        targetFollowers.Remove(currentUserId);

        currentUser.Following = JsonSerializer.Serialize(currentFollowing);
        currentUser.UpdatedAt = DateTime.UtcNow;

        targetUser.Followers = JsonSerializer.Serialize(targetFollowers);
        targetUser.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PinTweetAsync(string userId, string tweetId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PinnedTweet = tweetId;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnpinTweetAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return false;

        user.PinnedTweet = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
