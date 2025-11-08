namespace TeamFlowAPI.Models.Entities;

public class Team
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public List<TeamMember> TeamMembers { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
}