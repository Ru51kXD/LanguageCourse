using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using WebApplication15.Models;
using WebApplication15.Services;
using WebApplication15.Data;
using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using System.IO;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using System.Collections.Generic;

// Вспомогательный класс, используемый для логгера
internal class ProgramHelper { }

// Главная точка входа в программу
public static class Program
{
    public static void Main(string[] args)
    {
        CreateApplication(args);
    }

    // Метод для использования при запуске веб-приложения
    private static void CreateApplication(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpContextAccessor();

        // Добавление сервисов локализации (должен быть перед AddMvc)
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        
        // Регистрация сервисов
        builder.Services.AddScoped<WebApplication15.Models.LocalizationService>();
        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<LanguageService>();
        builder.Services.AddScoped<TestService>();
        builder.Services.AddScoped<ThemeService>();
        builder.Services.AddScoped<TestDataService>();
        builder.Services.AddScoped<VideoService>();

        // Настройка MVC с поддержкой локализации
        builder.Services.AddMvc()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (type, factory) =>
                    factory.Create(typeof(WebApplication15.Models.LocalizationService));
            });

        // Получаем строку подключения
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        // Настройка контекста базы данных
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Настройка сессий для авторизации
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            // Добавляем настройки для режима разработки
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        });

        // Настройка защиты данных для сессий
        builder.Services.AddDataProtection()
            .SetApplicationName("WebApplication15")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });

        // Настройка Identity
        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequiredLength = 6;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Настройка поддерживаемых культур
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("ru"),
                new CultureInfo("kk"),
                new CultureInfo("en"),
                new CultureInfo("tr")
            };

            options.DefaultRequestCulture = new RequestCulture("ru");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            
            // Настройка провайдеров для определения культуры
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                // Порядок провайдеров имеет значение - первый имеет высший приоритет
                new CookieRequestCultureProvider
                {
                    CookieName = CookieRequestCultureProvider.DefaultCookieName,
                    Options = options
                },
                new QueryStringRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });

        var app = builder.Build();

        // Инициализация базы данных
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
            
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = services.GetRequiredService<ILogger<ProgramHelper>>();
            
            // Создание ролей
            string[] roleNames = { "Admin", "Teacher", "Student" };
            foreach (var roleName in roleNames)
            {
                if (!roleManager.RoleExistsAsync(roleName).Result)
                {
                    roleManager.CreateAsync(new IdentityRole(roleName)).Wait();
                }
            }
            
            // Создание администратора, если его нет
            var admin = userManager.FindByNameAsync("admin").Result;
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User",
                    RegistrationDate = DateTime.Now,
                    IsActive = true
                };
                
                userManager.CreateAsync(admin, "Admin123!").Wait();
                userManager.AddToRoleAsync(admin, "Admin").Wait();
            }
            
            // Создание языков, если их нет
            if (!context.Languages.Any())
            {
                context.Languages.AddRange(
                    new Language { Name = "Английский", Code = "en", Icon = "🇬🇧" },
                    new Language { Name = "Казахский", Code = "kk", Icon = "🇰🇿" },
                    new Language { Name = "Турецкий", Code = "tr", Icon = "🇹🇷" },
                    new Language { Name = "Русский", Code = "ru", Icon = "🇷🇺" }
                );
                context.SaveChanges();
                logger.LogInformation("Языки добавлены в базу данных");
            }
            
            // Создание уровней языков, если их нет
            if (!context.LanguageLevels.Any())
            {
                var languages = context.Languages.ToList();
                var levels = new[] { "A1", "A2", "B1", "B2", "C1", "C2" };
                
                foreach (var language in languages)
                {
                    for (int i = 0; i < levels.Length; i++)
                    {
                        context.LanguageLevels.Add(new LanguageLevel
                        {
                            Name = levels[i],
                            Level = i + 1,
                            Description = GetLevelDescription(levels[i]),
                            LanguageId = language.Id
                        });
                    }
                }
                context.SaveChanges();
                logger.LogInformation("Уровни языков добавлены в базу данных");
            }
            
            // Проверка наличия тестов, создание только при отсутствии
            if (!context.Tests.Any())
            {
                // Инициализация тестов
                var testService = services.GetRequiredService<TestService>();
                testService.SeedDemoTestsAsync().Wait();
                logger.LogInformation("Демо-тесты созданы");
            }
        }

        // Метод для получения описания уровня
        string GetLevelDescription(string level)
        {
            return level switch
            {
                "A1" => "Начальный",
                "A2" => "Элементарный",
                "B1" => "Средний",
                "B2" => "Выше среднего",
                "C1" => "Продвинутый",
                "C2" => "Свободное владение",
                _ => "Неизвестный уровень"
            };
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Добавляем локализацию в начало конвейера
        var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
        if (localizationOptions != null)
        {
            app.UseRequestLocalization(localizationOptions.Value);
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();

        if (app.Environment.IsDevelopment())
        {
            // Отладочная информация о локализации
            app.Use(async (context, next) =>
            {
                var currentCulture = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture?.Name;
                //Console.WriteLine($"Current culture: {currentCulture}");
                await next();
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSession();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}
