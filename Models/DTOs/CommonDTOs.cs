namespace TwitterClone.Api.Models.DTOs;

// Auth DTOs
public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

// User DTOs
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string PhotoURL { get; set; } = string.Empty;
    public string? CoverPhotoURL { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public bool Verified { get; set; }
    public string? Theme { get; set; }
    public string? Accent { get; set; }
    public List<string> Following { get; set; } = new();
    public List<string> Followers { get; set; } = new();
    public int TotalTweets { get; set; }
    public int TotalPhotos { get; set; }
    public string? PinnedTweet { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateUserProfileRequest
{
    public string? Name { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public string? Location { get; set; }
    public string? PhotoURL { get; set; }
    public string? CoverPhotoURL { get; set; }
}

public class UpdateUsernameRequest
{
    public string Username { get; set; } = string.Empty;
}

public class UpdateThemeRequest
{
    public string? Theme { get; set; }
    public string? Accent { get; set; }
}

// Tweet DTOs
public class ImageData
{
    public string Id { get; set; } = string.Empty;
    public string Src { get; set; } = string.Empty;
    public string Alt { get; set; } = string.Empty;
    public string? Type { get; set; }
}

public class ParentTweetInfo
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}

public class TweetDto
{
    public string Id { get; set; } = string.Empty;
    public string? Text { get; set; }
    public List<ImageData>? Images { get; set; }
    public ParentTweetInfo? Parent { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public List<string> UserLikes { get; set; } = new();
    public List<string> UserRetweets { get; set; } = new();
    public int UserReplies { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class CreateTweetRequest
{
    public string? Text { get; set; }
    public List<ImageData>? Images { get; set; }
    public ParentTweetInfo? Parent { get; set; }
}

// Bookmark DTOs
public class BookmarkDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TweetId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public TweetDto? Tweet { get; set; }
}

// Response wrappers
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public bool HasMore { get; set; }
    public string? NextCursor { get; set; }
}

public class ErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}
