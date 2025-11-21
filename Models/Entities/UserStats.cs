using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitterClone.Api.Models.Entities;

public class UserStats
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    // Store as JSON strings in SQL Server
    public string Likes { get; set; } = "[]"; // Tweet IDs user liked
    
    public string Tweets { get; set; } = "[]"; // Tweet IDs user retweeted
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
