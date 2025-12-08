namespace TeamFlowAPI.Models.Entities;

public class Attachment
{
    public long Id { get; set; }
    public long? TaskId { get; set; }
    public long OrgId { get; set; }
    public long? UploadedBy { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Filename { get; set; }
    public long? Size { get; set; }
    public string? ContentType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public TaskItem? Task { get; set; }
    public Organization Organization { get; set; } = null!;
    public User? Uploader { get; set; }
}