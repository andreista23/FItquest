using FitQuest.Models;

public class TrainerActivityAssignment
{
    public int Id { get; set; }

    public int TrainerActivityId { get; set; }
    public TrainerActivity TrainerActivity { get; set; }

    public int UserId { get; set; }
    public User User { get; set; }

    public bool IsCompleted { get; set; } = false;

    // ✅ NEW
    public string? ProofPath { get; set; }          // ex: "trainer_proofs/12_99.pdf"
    public string? ProofMessage { get; set; }       // text optional
    public DateTime? CompletedAt { get; set; }
}