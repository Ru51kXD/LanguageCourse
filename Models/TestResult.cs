using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class TestResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int TestId { get; set; }

        [MaxLength(100)]
        public string TestTitle { get; set; } = string.Empty;

        // Свойство Title с геттером и сеттером для совместимости
        [NotMapped]
        public string Title 
        { 
            get => TestTitle; 
            set => TestTitle = value; 
        }

        [MaxLength(50)]
        public string Language { get; set; } = string.Empty;

        [MaxLength(10)]
        public string Level { get; set; } = string.Empty;

        public int Score { get; set; }

        public int MaxScore { get; set; }

        [Range(0, 100)]
        public double Percentage { get; set; }

        public int CorrectAnswers { get; set; }

        public int TotalQuestions { get; set; }

        public bool IsPassed { get; set; }

        public DateTime CompletedDate { get; set; } = DateTime.Now;

        // Длительность прохождения теста в секундах
        public int DurationSeconds { get; set; } = 0;

        // Свойство Duration для обратной совместимости
        [NotMapped]
        public TimeSpan Duration => TimeSpan.FromSeconds(DurationSeconds);

        // Навигационные свойства
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey("TestId")]
        public virtual Test? Test { get; set; }

        // Вычисляемые свойства
        [NotMapped]
        public string FormattedScore => $"{Score}/{MaxScore} ({Percentage:F1}%)";

        [NotMapped]
        public string Status => IsPassed ? "Пройден" : "Не пройден";

        [NotMapped]
        public string StatusColor => IsPassed ? "success" : "danger";

        [NotMapped]
        public string FormattedDate => CompletedDate.ToString("dd.MM.yyyy HH:mm");
    }
} 