namespace TeamFlowAPI.Models.Entities;

public class TaskItem
{
    public long Id { get; set; }
    public long OrgId { get; set; }
    public long? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "backlog";
    public string Priority { get; set; } = "medium";
    public DateTimeOffset? DueDate { get; set; }
    public decimal? EstimatedHours { get; set; }
    public long? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public int OrderIndex { get; set; } = 0;
    public long? ParentTaskId { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation properties
    public Organization Organization { get; set; } = null!;
    public Project? Project { get; set; }
    public User? Creator { get; set; }
    public TaskItem? ParentTask { get; set; }
    public List<TaskItem> Subtasks { get; set; } = new();
    public List<TaskAssignment> TaskAssignments { get; set; } = new();
    public List<Comment> Comments { get; set; } = new();
    public List<Attachment> Attachments { get; set; } = new();
}