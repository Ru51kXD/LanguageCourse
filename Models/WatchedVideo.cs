using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication15.Models
{
    public class WatchedVideo
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int VideoId { get; set; }

        [Required]
        public DateTime WatchedDate { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; } = null!;

        [ForeignKey("VideoId")]
        public virtual Video Video { get; set; } = null!;
    }
} 