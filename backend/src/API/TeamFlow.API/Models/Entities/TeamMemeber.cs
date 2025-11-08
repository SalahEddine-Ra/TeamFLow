namespace TeamFlowAPI.Models.Entities;

public class TeamMember
{
    public long Id { get; set; }
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public string? Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}