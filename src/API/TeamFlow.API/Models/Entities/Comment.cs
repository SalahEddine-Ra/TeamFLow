namespace TeamFlowAPI.Models.Entities;
public class Comment
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long OrgId { get; set; }
    public long? UserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
    public string? MentionedUserIds { get; set; } // JSON array

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public User? User { get; set; }
}