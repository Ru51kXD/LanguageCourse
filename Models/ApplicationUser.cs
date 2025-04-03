/*
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Display(Name = "Выбранный язык")]
        public string PreferredLanguage { get; set; } = "kk"; // kk - казахский (по умолчанию)

        [Display(Name = "Темный режим")]
        public bool DarkMode { get; set; } = true; // Темный режим по умолчанию

        [Display(Name = "Имя")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Фамилия")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Дата регистрации")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        public string ProfilePictureUrl { get; set; } = string.Empty;

        // Навигационное свойство для результатов тестов
        public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
        public virtual ICollection<WatchedVideo> WatchedVideos { get; set; } = new List<WatchedVideo>();

        [NotMapped]
        public bool IsAdmin { get; set; }

        [NotMapped]
        public int TestsCompleted { get; set; }

        [NotMapped]
        public int WatchedVideosCount { get; set; }

        [NotMapped]
        public IList<string> Roles { get; set; } = new List<string>();

        [NotMapped]
        [Display(Name = "Полное имя")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
*/ 