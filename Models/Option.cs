/*
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class Option
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        [ForeignKey("Question")]
        public int QuestionId { get; set; }

        // Навигационные свойства
        public virtual Question? Question { get; set; }
    }
}
*/ 