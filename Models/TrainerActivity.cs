using FitQuest.Models;

public class TrainerActivity
{
    public int Id { get; set; }

    public int TrainerProfileId { get; set; }
    public TrainerProfile TrainerProfile { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TrainerActivityAssignment> Assignments { get; set; }
}
