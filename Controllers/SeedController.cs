using Microsoft.AspNetCore.Mvc;
using WebApplication15.Data;
using WebApplication15.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;

namespace WebApplication15.Controllers
{
    public class SeedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Seed/Videos
        public async Task<IActionResult> Videos()
        {
            // Проверяем, есть ли уже видео в базе
            var existingVideos = await _context.Videos.CountAsync();
            if (existingVideos > 0)
            {
                return Content($"В базе данных уже есть {existingVideos} видеоуроков. Удалите их перед новым заполнением.");
            }

            // Получаем все уровни языков для добавления видео
            var languageLevels = await _context.LanguageLevels
                .Include(ll => ll.Language)
                .ToListAsync();

            if (languageLevels.Count == 0)
            {
                return Content("Ошибка: В базе данных отсутствуют уровни языков, необходимые для добавления видео.");
            }

            // Для каждого уровня языка добавляем несколько видео
            foreach (var level in languageLevels)
            {
                // Добавляем 3 видео для каждого уровня языка
                for (int i = 1; i <= 3; i++)
                {
                    var video = new Video
                    {
                        Title = $"Урок {i} - {level.Language.Name} - Уровень {level.Name}",
                        Description = $"Обучающий видеоурок для изучения {level.Language.Name} языка на уровне {level.Name}. Часть {i}.",
                        VideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ", // Тестовое видео YouTube
                        ThumbnailUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/maxresdefault.jpg",
                        LanguageLevelId = level.Id,
                        CreatedDate = DateTime.Now.AddDays(-i),
                        Duration = $"{new Random().Next(10, 60)} мин",
                        IsActive = true,
                        IsFeatured = i == 1 // Первое видео будет рекомендованным
                    };

                    _context.Videos.Add(video);
                }
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction("Index", "Video");
        }
    }
} 