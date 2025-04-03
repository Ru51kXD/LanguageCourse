/*
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

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Добавление сервисов локализации
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

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

// Регистрация сервисов
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LanguageService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<TestDataService>();
builder.Services.AddScoped<VideoService>();
builder.Services.AddScoped<WebApplication15.Models.LocalizationService>();

// Настройка MVC с поддержкой локализации
builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(LocalizationService));
    });

// Настройка поддерживаемых культур
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("kk"),
        new CultureInfo("en"),
        new CultureInfo("tr")
    };

    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
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
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    // Создание ролей
    string[] roleNames = { "Admin", "Teacher", "Student" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    
    // Создание администратора, если его нет
    var admin = await userManager.FindByNameAsync("admin");
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
        
        await userManager.CreateAsync(admin, "Admin123!");
        await userManager.AddToRoleAsync(admin, "Admin");
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
        await context.SaveChangesAsync();
        logger.LogInformation("Языки добавлены в базу данных");
    }
    
    // Создание уровней языков, если их нет
    if (!context.LanguageLevels.Any())
    {
        var languages = await context.Languages.ToListAsync();
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
        await context.SaveChangesAsync();
        logger.LogInformation("Уровни языков добавлены в базу данных");
    }
    
    // Проверка наличия тестов, создание только при отсутствии
    if (!context.Tests.Any())
    {
        // Инициализация тестов
        var testService = services.GetRequiredService<TestService>();
        await testService.SeedDemoTestsAsync();
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

app.UseHttpsRedirection();
app.UseStaticFiles();

// Настройка локализации в HTTP pipeline
var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>();
if (localizationOptions != null)
{
    app.UseRequestLocalization(localizationOptions.Value);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
*/
