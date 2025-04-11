using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication15.Data;
using WebApplication15.Models;

namespace WebApplication15.Services
{
    public class VideoService
    {
        private readonly ApplicationDbContext _context;

        public VideoService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Получить все видео
        public async Task<List<Video>> GetAllVideosAsync()
        {
            return await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .OrderBy(v => v.LanguageLevel.Language.Name)
                .ThenBy(v => v.LanguageLevel.Name)
                .ToListAsync();
        }

        // Получить все видео с отметкой просмотра
        public async Task<List<Video>> GetVideosWithWatchStatusAsync(string userId)
        {
            var videos = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .OrderBy(v => v.LanguageLevel.Language.Name)
                .ThenBy(v => v.LanguageLevel.Name)
                .ToListAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                var watchedVideoIds = await _context.WatchedVideos
                    .Where(w => w.UserId == userId)
                    .Select(w => w.VideoId)
                    .ToListAsync();

                foreach (var video in videos)
                {
                    video.IsWatched = watchedVideoIds.Contains(video.Id);
                }
            }

            return videos;
        }

        // Получить видео по ID
        public async Task<Video?> GetVideoByIdAsync(int id, string? userId = null)
        {
            var video = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(v => v.Id == id);
                
            if (video != null && !string.IsNullOrEmpty(userId))
            {
                video.IsWatched = await _context.WatchedVideos
                    .AnyAsync(w => w.VideoId == id && w.UserId == userId);
            }

            return video;
        }

        // Получить связанные видео
        public async Task<List<Video>> GetRelatedVideosAsync(int videoId, string? userId = null)
        {
            var video = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .FirstOrDefaultAsync(v => v.Id == videoId);
                
            if (video == null)
                return new List<Video>();
                
            // Сначала ищем видео того же уровня
            var relatedVideos = await _context.Videos
                .Include(v => v.LanguageLevel)
                    .ThenInclude(ll => ll.Language)
                .Where(v => v.Id != videoId && v.LanguageLevelId == video.LanguageLevelId)
                .OrderBy(v => v.Title)
                .Take(6)
                .ToListAsync();
                
            // Если недостаточно видео того же уровня, добавляем видео того же языка, но разных уровней
            if (relatedVideos.Count < 6 && video.LanguageLevel?.Language != null)
            {
                var languageId = video.LanguageLevel.Language.Id;
                var additionalVideos = await _context.Videos
                    .Include(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                    .Where(v => v.Id != videoId && 
                                v.LanguageLevel.Language.Id == languageId && 
                                v.LanguageLevelId != video.LanguageLevelId)
                    .OrderBy(v => v.Title)
                    .Take(6 - relatedVideos.Count)
                    .ToListAsync();
                    
                relatedVideos.AddRange(additionalVideos);
            }
            
            // Если все еще недостаточно видео, добавляем видео любого языка
            if (relatedVideos.Count < 3)
            {
                var anyVideos = await _context.Videos
                    .Include(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                    .Where(v => v.Id != videoId && 
                                !relatedVideos.Select(rv => rv.Id).Contains(v.Id))
                    .OrderByDescending(v => v.CreatedDate)
                    .Take(6 - relatedVideos.Count)
                    .ToListAsync();
                    
                relatedVideos.AddRange(anyVideos);
            }

            if (!string.IsNullOrEmpty(userId))
            {
                var watchedVideoIds = await _context.WatchedVideos
                    .Where(w => w.UserId == userId)
                    .Select(w => w.VideoId)
                    .ToListAsync();

                foreach (var relatedVideo in relatedVideos)
                {
                    relatedVideo.IsWatched = watchedVideoIds.Contains(relatedVideo.Id);
                }
            }

            return relatedVideos;
        }

        // Отметить видео как просмотренное
        public async Task MarkVideoAsWatchedAsync(int videoId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return;

            // Проверяем, просмотрено ли уже видео
            var alreadyWatched = await _context.WatchedVideos
                .AnyAsync(wv => wv.VideoId == videoId && wv.UserId == userId);

            if (!alreadyWatched)
            {
                var watched = new WatchedVideo
                {
                    VideoId = videoId,
                    UserId = userId,
                    WatchedDate = DateTime.Now
                };
                
                _context.WatchedVideos.Add(watched);
            }
            
            await _context.SaveChangesAsync();
        }

        // Получить список просмотренных видео пользователя
        public async Task<List<WatchedVideo>> GetUserWatchedVideosAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return new List<WatchedVideo>();

            return await _context.WatchedVideos
                .Include(w => w.Video)
                    .ThenInclude(v => v.LanguageLevel)
                        .ThenInclude(ll => ll.Language)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.WatchedDate)
                .ToListAsync();
        }

        // Отметить видео как просмотренное (публичный метод)
        public async Task<bool> MarkAsWatched(int videoId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            // Проверяем существование видео
            var videoExists = await _context.Videos.AnyAsync(v => v.Id == videoId);
            if (!videoExists)
                return false;

            // Проверяем, просмотрено ли уже видео
            var existingRecord = await _context.WatchedVideos
                .FirstOrDefaultAsync(wv => wv.VideoId == videoId && wv.UserId == userId);

            if (existingRecord == null)
            {
                var watched = new WatchedVideo
                {
                    VideoId = videoId,
                    UserId = userId,
                    WatchedDate = DateTime.Now
                };
                
                _context.WatchedVideos.Add(watched);
                await _context.SaveChangesAsync();
            }

            return true;
        }

        // Проверка просмотрено ли видео
        public async Task<bool> IsWatched(int videoId, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.WatchedVideos
                .AnyAsync(wv => wv.VideoId == videoId && wv.UserId == userId);
        }

        // Получить URL для миниатюры
        public async Task<string> GetThumbnailUrlFromVideoUrl(string videoUrl)
        {
            // Извлечение ID видео из URL YouTube
            string youtubeId = ExtractYouTubeVideoId(videoUrl);
            if (!string.IsNullOrEmpty(youtubeId))
            {
                // Возвращаем URL миниатюры в максимальном разрешении
                return $"https://img.youtube.com/vi/{youtubeId}/maxresdefault.jpg";
            }

            // Извлечение ID видео из URL Vimeo
            string vimeoId = ExtractVimeoVideoId(videoUrl);
            if (!string.IsNullOrEmpty(vimeoId))
            {
                try
                {
                    // Для Vimeo нужно получить информацию через API
                    // Но так как это требует API ключ, просто возвращаем заглушку
                    return $"https://vumbnail.com/{vimeoId}.jpg";
                }
                catch
                {
                    // Если не удалось получить миниатюру Vimeo, возвращаем стандартную
                }
            }

            // Если это не YouTube/Vimeo или не удалось извлечь ID
            return "/images/default-thumbnail.jpg";
        }

        // Получить URL для встраивания
        public async Task<string> GetEmbedUrlFromVideoUrl(string videoUrl)
        {
            // Извлечение ID видео из URL YouTube
            string youtubeId = ExtractYouTubeVideoId(videoUrl);
            if (!string.IsNullOrEmpty(youtubeId))
            {
                // Возвращаем URL для встраивания
                return $"https://www.youtube.com/embed/{youtubeId}";
            }

            // Если это не YouTube или не удалось извлечь ID
            return videoUrl;
        }

        // Извлечение ID видео из URL YouTube
        private string ExtractYouTubeVideoId(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                // Форматы YouTube URL
                // https://www.youtube.com/watch?v=VIDEO_ID
                // https://youtu.be/VIDEO_ID
                // https://www.youtube.com/embed/VIDEO_ID

                string videoId = string.Empty;

                if (url.Contains("youtu.be/"))
                {
                    videoId = url.Split(new[] { "youtu.be/" }, StringSplitOptions.None)[1];
                }
                else if (url.Contains("youtube.com/watch"))
                {
                    // Ищем параметр v=
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    videoId = query["v"];
                }
                else if (url.Contains("youtube.com/embed/"))
                {
                    videoId = url.Split(new[] { "youtube.com/embed/" }, StringSplitOptions.None)[1];
                }

                // Очищаем ID от дополнительных параметров
                if (!string.IsNullOrEmpty(videoId) && videoId.Contains("&"))
                {
                    videoId = videoId.Split('&')[0];
                }

                return videoId;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Извлечение ID видео из URL Vimeo
        private string ExtractVimeoVideoId(string url)
        {
            if (string.IsNullOrEmpty(url))
                return string.Empty;

            try
            {
                // Форматы Vimeo URL
                // https://vimeo.com/VIDEO_ID
                // https://player.vimeo.com/video/VIDEO_ID

                string videoId = string.Empty;

                if (url.Contains("vimeo.com/"))
                {
                    if (url.Contains("player.vimeo.com/video/"))
                    {
                        videoId = url.Split(new[] { "player.vimeo.com/video/" }, StringSplitOptions.None)[1];
                    }
                    else
                    {
                        videoId = url.Split(new[] { "vimeo.com/" }, StringSplitOptions.None)[1];
                    }

                    // Очищаем ID от дополнительных параметров
                    if (!string.IsNullOrEmpty(videoId) && videoId.Contains("?"))
                    {
                        videoId = videoId.Split('?')[0];
                    }
                }

                return videoId;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Удалить записи о просмотренных видео
        public async Task<bool> DeleteWatchedVideoRecords(int videoId)
        {
            var watchedRecords = await _context.WatchedVideos
                .Where(wv => wv.VideoId == videoId)
                .ToListAsync();

            if (watchedRecords.Any())
            {
                _context.WatchedVideos.RemoveRange(watchedRecords);
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
} 