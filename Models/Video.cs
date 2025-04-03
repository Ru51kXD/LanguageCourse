using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class Video
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название видео")]
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Описание")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Выберите уровень языка")]
        [Display(Name = "Уровень языка")]
        public int LanguageLevelId { get; set; }

        [Required(ErrorMessage = "Введите URL видео")]
        [Display(Name = "URL видео")]
        public string VideoUrl { get; set; } = string.Empty;

        [Display(Name = "URL миниатюры")]
        public string ThumbnailUrl { get; set; } = string.Empty;

        [Display(Name = "Длительность")]
        public string Duration { get; set; } = string.Empty;

        [Display(Name = "Дата создания")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Активно")]
        public bool IsActive { get; set; } = true;

        [NotMapped]
        [Display(Name = "Просмотрено")]
        public bool IsWatched { get; set; }

        [NotMapped]
        public int PassRate { get; set; }

        [ForeignKey("LanguageLevelId")]
        public virtual LanguageLevel LanguageLevel { get; set; } = null!;

        [NotMapped]
        public string Url => VideoUrl;

        [NotMapped]
        public virtual Language Language => LanguageLevel?.Language;

        public virtual ICollection<WatchedVideo> WatchedVideos { get; set; }

        public Video()
        {
            WatchedVideos = new HashSet<WatchedVideo>();
            CreatedDate = DateTime.Now;
        }
    }
} 