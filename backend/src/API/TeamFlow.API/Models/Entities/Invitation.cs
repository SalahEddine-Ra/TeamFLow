namespace TeamFlowAPI.Models.Entities;

public class Invitation
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public long? InviterUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string Token { get; set; } = string.Empty;
    public string? TokenHash { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public long? AcceptedByUserId { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User? InviterUser { get; set; }
    public User? AcceptedByUser { get; set; }
}