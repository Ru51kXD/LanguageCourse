using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using WebApplication15.Models;
using WebApplication15.Data;

namespace WebApplication15.Services
{
    public class ThemeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ThemeService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // Получение текущей темы для пользователя
        public async Task<bool> GetUserTheme(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.DarkMode ?? true; // По умолчанию темная тема
        }

        // Получение текущей темы из сессии или cookie
        public bool GetCurrentTheme()
        {
            var isDarkTheme = _httpContextAccessor.HttpContext?.Session.GetString("Theme") == "dark";
            return isDarkTheme; // true - темная тема, false - светлая тема
        }

        // Установка темы для пользователя
        public async Task ToggleUserTheme(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.DarkMode = !user.DarkMode;
                await _context.SaveChangesAsync();
            }
            
            // Также изменяем тему в сессии
            SetThemeForGuest(user?.DarkMode ?? true);
        }

        // Установка темы для неавторизованного пользователя
        public void SetThemeForGuest(bool isDarkTheme)
        {
            _httpContextAccessor.HttpContext?.Session.SetString("Theme", isDarkTheme ? "dark" : "light");
        }

        // Переключение темы
        public void ToggleTheme()
        {
            var currentTheme = GetCurrentTheme();
            SetThemeForGuest(!currentTheme);
        }
    }
} 