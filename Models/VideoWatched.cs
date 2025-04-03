using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class VideoWatched
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int VideoId { get; set; }

        [Required]
        public DateTime WatchedDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("VideoId")]
        public virtual Video Video { get; set; }

        // Процент просмотра (может использоваться для отслеживания прогресса)
        public int WatchPercentage { get; set; } = 100;

        // Помечено ли как завершенное
        public bool IsCompleted { get; set; } = true;
    }
} 