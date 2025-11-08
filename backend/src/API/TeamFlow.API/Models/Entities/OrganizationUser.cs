namespace TeamFlowAPI.Models.Entities;

public class OrganizationUser
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public long UserId { get; set; }
    public string Role { get; set; } = "member";
    public string InviteStatus { get; set; } = "pending";
    public DateTimeOffset? InvitedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}