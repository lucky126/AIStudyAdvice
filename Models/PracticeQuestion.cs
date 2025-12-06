using System.ComponentModel.DataAnnotations;

namespace Study.Models
{
    public class PracticeQuestion
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string PaperId { get; set; } = "";
        public string UserId { get; set; } = "demo_user";
        public string Content { get; set; } = "";
        public string? CorrectAnswer { get; set; }
        public string? UserAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public string? KnowledgePoint { get; set; }
        public string? QuestionType { get; set; }
        public string? Options { get; set; }
        public string? Subject { get; set; }
        public int Grade { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    }
}