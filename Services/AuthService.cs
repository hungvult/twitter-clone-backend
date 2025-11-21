using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TwitterClone.Api.Data;
using TwitterClone.Api.Models.DTOs;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken);
    Task<AuthResponse?> RefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;

    public AuthService(ApplicationDbContext context, IConfiguration configuration, IMapper mapper)
    {
        _context = context;
        _configuration = configuration;
        _mapper = mapper;
    }

    public async Task<AuthResponse> AuthenticateWithGoogleAsync(string idToken)
    {
        // Verify Google token
        var googleClientId = _configuration["Authentication:Google:ClientId"];
        GoogleJsonWebSignature.Payload payload;
        
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId! }
            });
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }

        // Check if user exists
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == payload.Email);

        if (user == null)
        {
            // Create new user
            var username = await GenerateUniqueUsernameAsync(payload.Name ?? payload.Email.Split('@')[0]);
            
            user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Name = payload.Name ?? "User",
                Username = username,
                Email = payload.Email,
                PhotoURL = payload.Picture ?? "",
                Verified = false,
                Following = "[]",
                Followers = "[]",
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);

            // Create UserStats for new user
            var userStats = new UserStats
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                Likes = "[]",
                Tweets = "[]"
            };

            _context.UserStats.Add(userStats);
            await _context.SaveChangesAsync();
        }

        // Generate JWT tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();

        var userDto = _mapper.Map<UserDto>(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = userDto
        };
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        // TODO: Implement refresh token validation from database
        // For now, return null
        await Task.CompletedTask;
        return null;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string baseName)
    {
        // Clean base name
        var cleanName = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        if (string.IsNullOrEmpty(cleanName))
            cleanName = "user";

        // Ensure it starts with a letter
        if (!char.IsLetter(cleanName[0]))
            cleanName = "u" + cleanName;

        // Try with random 4 digits
        var random = new Random();
        for (int i = 0; i < 100; i++)
        {
            var randomDigits = random.Next(1000, 10000);
            var username = (cleanName + randomDigits).ToLower();
            
            // Ensure length is between 4-15
            if (username.Length > 15)
                username = username.Substring(0, 15);
            
            var exists = await _context.Users.AnyAsync(u => u.Username == username);
            if (!exists)
                return username;
        }

        // Fallback: use GUID
        return (cleanName + Guid.NewGuid().ToString("N").Substring(0, 8)).Substring(0, 15);
    }

    private string GenerateAccessToken(User user)
    {
        var secretKey = _configuration["Authentication:Jwt:SecretKey"]!;
        var issuer = _configuration["Authentication:Jwt:Issuer"];
        var audience = _configuration["Authentication:Jwt:Audience"];
        var expirationMinutes = int.Parse(_configuration["Authentication:Jwt:AccessTokenExpirationMinutes"] ?? "60");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("IsAdmin", (user.Username == "ccrsxx").ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        // Generate a simple refresh token (in production, store this in database)
        return Guid.NewGuid().ToString("N");
    }
}
