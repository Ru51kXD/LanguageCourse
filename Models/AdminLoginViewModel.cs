using System.ComponentModel.DataAnnotations;

namespace WebApplication15.Models
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Поле Имя пользователя обязательно для заполнения")]
        [Display(Name = "Имя пользователя")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Поле Пароль обязательно для заполнения")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }
} 