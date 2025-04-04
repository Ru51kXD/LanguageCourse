using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class LanguageLevel
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(10)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 6)]
        public int Level { get; set; }

        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [ForeignKey("Language")]
        public int LanguageId { get; set; }

        // Навигационные свойства
        public virtual Language? Language { get; set; }

        public virtual ICollection<Test> Tests { get; set; } = new List<Test>();
        
        public virtual ICollection<Video> Videos { get; set; } = new List<Video>();

        // Вычисляемые свойства
        [NotMapped]
        public string DisplayName => $"{Name} - {Description}";

        [NotMapped]
        public string LanguageName => Language?.Name ?? "Неизвестный язык";

        [NotMapped]
        public string FullName => $"{LanguageName} - {Name}";
    }
} 