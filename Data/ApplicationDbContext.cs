using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication15.Models;

namespace WebApplication15.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Включаем детальное логирование SQL-запросов для отладки
            this.Database.GetDbConnection().StateChange += (sender, e) => 
            {
                if (e.CurrentState == System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Соединение с базой данных открыто");
                }
            };
        }

        public DbSet<Language> Languages { get; set; }
        public DbSet<LanguageLevel> LanguageLevels { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Option> Options { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<WatchedVideo> WatchedVideos { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<LanguageLevel>()
                .HasOne(ll => ll.Language)
                .WithMany()
                .HasForeignKey(ll => ll.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Test>()
                .HasOne(t => t.LanguageLevel)
                .WithMany()
                .HasForeignKey(t => t.LanguageLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Question>()
                .HasOne(q => q.Test)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Option>()
                .HasOne(o => o.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(o => o.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.Test)
                .WithMany(t => t.TestResults)
                .HasForeignKey(tr => tr.TestId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TestResult>()
                .HasOne(tr => tr.User)
                .WithMany(u => u.TestResults)
                .HasForeignKey(tr => tr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Video>()
                .HasOne(v => v.LanguageLevel)
                .WithMany()
                .HasForeignKey(v => v.LanguageLevelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WatchedVideo>()
                .HasOne(wv => wv.Video)
                .WithMany(v => v.WatchedVideos)
                .HasForeignKey(wv => wv.VideoId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<WatchedVideo>()
                .HasOne(wv => wv.User)
                .WithMany(u => u.WatchedVideos)
                .HasForeignKey(wv => wv.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 