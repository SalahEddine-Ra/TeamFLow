namespace TeamFlowAPI.Models.Entities;

public class PlatformAdmin
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public long? GrantedByUserId { get; set; }
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public long? RevokedByUserId { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public User? GrantedByUser { get; set; }
    public User? RevokedByUser { get; set; }
}