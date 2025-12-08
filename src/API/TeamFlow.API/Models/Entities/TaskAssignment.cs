namespace TeamFlowAPI.Models.Entities;

public class TaskAssignment
{
    public long Id { get; set; }
    public long TaskId { get; set; }
    public long UserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public TaskItem Task { get; set; } = null!;
    public User User { get; set; } = null!;
}