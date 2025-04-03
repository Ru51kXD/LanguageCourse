using System.ComponentModel.DataAnnotations;

namespace WebApplication15.Models
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Введите имя пользователя")]
        [Display(Name = "Имя пользователя")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите Email")]
        [EmailAddress(ErrorMessage = "Неверный формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
} 