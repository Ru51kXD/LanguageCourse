using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication15.Models
{
    public class Language
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Название")]
        public string Name { get; set; }

        [Display(Name = "Код")]
        public string Code { get; set; }

        [Display(Name = "Иконка")]
        public string Icon { get; set; }

        public virtual ICollection<Test> Tests { get; set; }
        public virtual ICollection<Video> Videos { get; set; }

        public Language()
        {
            Tests = new HashSet<Test>();
            Videos = new HashSet<Video>();
        }
    }
} 