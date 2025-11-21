using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitterClone.Api.Models.Entities;

public class Bookmark
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string TweetId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
    
    [ForeignKey(nameof(TweetId))]
    public virtual Tweet Tweet { get; set; } = null!;
}
