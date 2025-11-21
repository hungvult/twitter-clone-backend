using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitterClone.Api.Models.Entities;

public class Tweet
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [MaxLength(560)]
    public string? Text { get; set; } // Nullable - can have just images
    
    // Store as JSON string in SQL Server
    public string? Images { get; set; } // JSON array of ImageData
    
    // Parent tweet info for replies
    public string? ParentId { get; set; }
    public string? ParentUsername { get; set; }
    
    [Required]
    public string CreatedBy { get; set; } = string.Empty;
    
    // Store as JSON strings in SQL Server
    public string UserLikes { get; set; } = "[]"; // Array of user IDs who liked
    
    public string UserRetweets { get; set; } = "[]"; // Array of user IDs who retweeted
    
    public int UserReplies { get; set; } = 0; // Reply count
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
}
