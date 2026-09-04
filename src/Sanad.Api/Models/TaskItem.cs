namespace Sanad.Api.Models;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.ToDo;
    public string? Tags { get; set; }
    public string? Project { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int Order { get; set; }
    
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}

public record TaskUpdateDto(Guid Id, TaskStatus Status, int Order);
