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

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        // Setup AutoMapper
        var config = new AutoMapper.MapperConfiguration(cfg =>
        {
            cfg.AddProfile<TwitterClone.Api.Mappings.MappingProfile>();
        });
        _mapper = config.CreateMapper();

        _userService = new UserService(_context, _mapper);
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Username = "testuser",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByIdAsync("user1");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("user1");
        result.Name.Should().Be("Test User");
        result.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenUserNotExists_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByIdAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByUsernameAsync_WhenUserExists_ReturnsUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Name = "Test User",
            Username = "testuser",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetUserByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task CheckUsernameAvailabilityAsync_WhenUsernameAvailable_ReturnsTrue()
    {
        // Act
        var result = await _userService.CheckUsernameAvailabilityAsync("newuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckUsernameAvailabilityAsync_WhenUsernameTaken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "existinguser",
            Name = "Test",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.CheckUsernameAvailabilityAsync("existinguser");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserExists_UpdatesProfile()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Name = "Old Name",
            Username = "testuser",
            Email = "test@example.com",
            Bio = "Old Bio",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new UpdateUserProfileRequest
        {
            Name = "New Name",
            Bio = "New Bio",
            Location = "New Location"
        };

        // Act
        var result = await _userService.UpdateUserProfileAsync("user1", request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Name");
        result.Bio.Should().Be("New Bio");
        result.Location.Should().Be("New Location");

        var updatedUser = await _context.Users.FindAsync("user1");
        updatedUser!.Name.Should().Be("New Name");
        updatedUser.Bio.Should().Be("New Bio");
        updatedUser.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserNotExists_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new UpdateUserProfileRequest { Name = "New Name" };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _userService.UpdateUserProfileAsync("nonexistent", request));
    }

    [Fact]
    public async Task UpdateUsernameAsync_WithValidUsername_UpdatesUsername()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "oldusername",
            Name = "Test",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UpdateUsernameAsync("user1", "newusername");

        // Assert
        result.Username.Should().Be("newusername");
        var updatedUser = await _context.Users.FindAsync("user1");
        updatedUser!.Username.Should().Be("newusername");
    }

    [Fact]
    public async Task UpdateUsernameAsync_WithInvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.UpdateUsernameAsync("user1", "ab")); // Too short
    }

    [Fact]
    public async Task UpdateUsernameAsync_WithInvalidCharacters_ThrowsArgumentException()
    {
        // Arrange
        var user = new User
        {
            Id = "user1",
            Username = "testuser",
            Name = "Test",
            Email = "test@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _userService.UpdateUsernameAsync("user1", "user-name")); // Invalid character
    }

    [Fact]
    public async Task FollowUserAsync_WhenNotAlreadyFollowing_AddsToFollowing()
    {
        // Arrange
        var currentUser = new User
        {
            Id = "user1",
            Username = "user1",
            Name = "User 1",
            Email = "user1@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var targetUser = new User
        {
            Id = "user2",
            Username = "user2",
            Name = "User 2",
            Email = "user2@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddRangeAsync(currentUser, targetUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.FollowUserAsync("user1", "user2");

        // Assert
        result.Should().BeTrue();
        var updatedCurrentUser = await _context.Users.FindAsync("user1");
        var following = JsonSerializer.Deserialize<List<string>>(updatedCurrentUser!.Following);
        following.Should().Contain("user2");

        var updatedTargetUser = await _context.Users.FindAsync("user2");
        var followers = JsonSerializer.Deserialize<List<string>>(updatedTargetUser!.Followers);
        followers.Should().Contain("user1");
    }

    [Fact]
    public async Task FollowUserAsync_WhenAlreadyFollowing_ReturnsTrue()
    {
        // Arrange
        var currentUser = new User
        {
            Id = "user1",
            Username = "user1",
            Name = "User 1",
            Email = "user1@example.com",
            Following = JsonSerializer.Serialize(new List<string> { "user2" }),
            Followers = "[]"
        };
        var targetUser = new User
        {
            Id = "user2",
            Username = "user2",
            Name = "User 2",
            Email = "user2@example.com",
            Following = "[]",
            Followers = JsonSerializer.Serialize(new List<string> { "user1" })
        };
        await _context.Users.AddRangeAsync(currentUser, targetUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.FollowUserAsync("user1", "user2");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnfollowUserAsync_WhenFollowing_RemovesFromFollowing()
    {
        // Arrange
        var currentUser = new User
        {
            Id = "user1",
            Username = "user1",
            Name = "User 1",
            Email = "user1@example.com",
            Following = JsonSerializer.Serialize(new List<string> { "user2" }),
            Followers = "[]"
        };
        var targetUser = new User
        {
            Id = "user2",
            Username = "user2",
            Name = "User 2",
            Email = "user2@example.com",
            Following = "[]",
            Followers = JsonSerializer.Serialize(new List<string> { "user1" })
        };
        await _context.Users.AddRangeAsync(currentUser, targetUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.UnfollowUserAsync("user1", "user2");

        // Assert
        result.Should().BeTrue();
        var updatedCurrentUser = await _context.Users.FindAsync("user1");
        var following = JsonSerializer.Deserialize<List<string>>(updatedCurrentUser!.Following);
        following.Should().NotContain("user2");
    }

    [Fact]
    public async Task GetAllUsersAsync_WithLimitAndOffset_ReturnsPaginatedUsers()
    {
        // Arrange
        var users = new List<User>();
        for (int i = 1; i <= 10; i++)
        {
            users.Add(new User
            {
                Id = $"user{i}",
                Username = $"user{i}",
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                Following = "[]",
                Followers = "[]"
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllUsersAsync(limit: 5, offset: 0);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithExcludeUserId_ExcludesUser()
    {
        // Arrange
        var user1 = new User
        {
            Id = "user1",
            Username = "user1",
            Name = "User 1",
            Email = "user1@example.com",
            Following = "[]",
            Followers = "[]"
        };
        var user2 = new User
        {
            Id = "user2",
            Username = "user2",
            Name = "User 2",
            Email = "user2@example.com",
            Following = "[]",
            Followers = "[]"
        };
        await _context.Users.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _userService.GetAllUsersAsync(limit: 10, offset: 0, excludeUserId: "user1");

        // Assert
        result.Should().NotContain(u => u.Id == "user1");
        result.Should().Contain(u => u.Id == "user2");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

