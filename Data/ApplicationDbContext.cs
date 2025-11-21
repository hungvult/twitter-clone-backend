using Microsoft.EntityFrameworkCore;
using TwitterClone.Api.Models.Entities;

namespace TwitterClone.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Tweet> Tweets { get; set; }
    public DbSet<Bookmark> Bookmarks { get; set; }
    public DbSet<UserStats> UserStats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.Stats)
                .WithOne(s => s.User)
                .HasForeignKey<UserStats>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Tweet entity configuration
        modelBuilder.Entity<Tweet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ParentId);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Tweets)
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Bookmark entity configuration
        modelBuilder.Entity<Bookmark>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.TweetId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Tweet)
                .WithMany(t => t.Bookmarks)
                .HasForeignKey(e => e.TweetId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // UserStats entity configuration
        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
        });
    }
}
