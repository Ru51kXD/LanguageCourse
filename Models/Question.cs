using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WebApplication15.Models
{
    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Текст вопроса обязателен")]
        [Display(Name = "Вопрос")]
        public string Text { get; set; }

        [Required]
        [Display(Name = "Тест")]
        public int TestId { get; set; }

        [Display(Name = "Правильный ответ")]
        public string CorrectAnswer { get; set; }

        [ForeignKey("TestId")]
        public virtual Test Test { get; set; }

        public virtual ICollection<Option> Options { get; set; }

        public Question()
        {
            Options = new HashSet<Option>();
        }

        // Вычисляемые свойства
        [NotMapped]
        public bool HasCorrectOption => Options.Any(o => o.IsCorrect);

        [NotMapped]
        public string TypeName => QuestionType switch
        {
            0 => "Одиночный выбор",
            1 => "Множественный выбор",
            2 => "Ввод текста",
            _ => "Неизвестный тип"
        };
    }
} 