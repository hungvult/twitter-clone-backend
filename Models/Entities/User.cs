using System.ComponentModel.DataAnnotations;

namespace TwitterClone.Api.Models.Entities;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(15)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    
    [MaxLength(160)]
    public string? Bio { get; set; }
    
    [MaxLength(500)]
    public string PhotoURL { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? CoverPhotoURL { get; set; }
    
    [MaxLength(100)]
    public string? Website { get; set; }
    
    [MaxLength(30)]
    public string? Location { get; set; }
    
    public bool Verified { get; set; } = false;
    
    [MaxLength(10)]
    public string? Theme { get; set; } // 'light' | 'dim' | 'dark'
    
    [MaxLength(10)]
    public string? Accent { get; set; } // 'blue' | 'yellow' | 'pink' | 'purple' | 'orange' | 'green'
    
    // Store as JSON string in SQL Server
    public string Following { get; set; } = "[]"; // Array of user IDs
    
    public string Followers { get; set; } = "[]"; // Array of user IDs
    
    public int TotalTweets { get; set; } = 0;
    
    public int TotalPhotos { get; set; } = 0;
    
    public string? PinnedTweet { get; set; } // Tweet ID
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Tweet> Tweets { get; set; } = new List<Tweet>();
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    public virtual UserStats? Stats { get; set; }
}
