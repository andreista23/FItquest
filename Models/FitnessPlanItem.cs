namespace FitQuest.Models
{
    public class FitnessPlanItem
    {
        public int Id { get; set; }

        public int FitnessPlanId { get; set; }
        public FitnessPlan FitnessPlan { get; set; }

        public int TrainerActivityId { get; set; }
        public TrainerActivity TrainerActivity { get; set; }

        public int Order { get; set; } // ordinea în plan
    }
}
