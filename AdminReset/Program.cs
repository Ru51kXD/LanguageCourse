using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AdminReset;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Запуск скрипта сброса пароля администратора...");
        
        try
        {
            // Путь к базе данных сайта
            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=LanguageCourses_New;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";

            // Создаем опции для контекста
            var optionsBuilder = new DbContextOptionsBuilder<AdminResetDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Создаем контекст
            using (var context = new AdminResetDbContext(optionsBuilder.Options))
            {
                // Создаем менеджер паролей
                var passwordHasher = new PasswordHasher<User>();

                // Ищем пользователя admin
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "admin");

                if (adminUser != null)
                {
                    Console.WriteLine("Найден пользователь admin, сбрасываем пароль на admin123");

                    // Устанавливаем новый пароль
                    adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, "admin123");

                    // Обновляем пользователя
                    context.Users.Update(adminUser);
                    await context.SaveChangesAsync();

                    // Проверяем роль Admin
                    var adminRoleExists = await CheckOrCreateAdminRole(context);
                    if (adminRoleExists)
                    {
                        // Проверяем, есть ли у пользователя роль Admin
                        var userRole = await context.UserRoles
                            .FirstOrDefaultAsync(ur => ur.UserId == adminUser.Id && 
                                                     ur.RoleId == context.Roles.First(r => r.Name == "Admin").Id);
                        
                        if (userRole == null)
                        {
                            // Добавляем пользователя в роль Admin
                            context.UserRoles.Add(new IdentityUserRole<string>
                            {
                                UserId = adminUser.Id,
                                RoleId = context.Roles.First(r => r.Name == "Admin").Id
                            });
                            
                            await context.SaveChangesAsync();
                            Console.WriteLine("Добавлена роль Admin пользователю admin");
                        }
                    }

                    Console.WriteLine("Пароль успешно сброшен. Используйте логин: admin, пароль: admin123");
                }
                else
                {
                    Console.WriteLine("Пользователь admin не найден. Создаем нового...");

                    // Создаем нового пользователя
                    var newAdmin = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "admin",
                        Email = "admin@example.com",
                        EmailConfirmed = true,
                        NormalizedUserName = "ADMIN",
                        NormalizedEmail = "ADMIN@EXAMPLE.COM",
                        FirstName = "Admin",
                        LastName = "User",
                        RegistrationDate = DateTime.Now,
                        IsActive = true,
                        SecurityStamp = Guid.NewGuid().ToString()
                    };

                    // Хешируем пароль
                    newAdmin.PasswordHash = passwordHasher.HashPassword(newAdmin, "admin123");

                    // Добавляем пользователя
                    context.Users.Add(newAdmin);
                    await context.SaveChangesAsync();

                    // Проверяем существование роли Admin
                    var adminRoleExists = await CheckOrCreateAdminRole(context);
                    if (adminRoleExists)
                    {
                        // Добавляем пользователя в роль Admin
                        context.UserRoles.Add(new IdentityUserRole<string>
                        {
                            UserId = newAdmin.Id,
                            RoleId = context.Roles.First(r => r.Name == "Admin").Id
                        });

                        await context.SaveChangesAsync();
                    }

                    Console.WriteLine("Администратор успешно создан. Используйте логин: admin, пароль: admin123");
                }
            }
            
            Console.WriteLine("Скрипт успешно выполнен!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при выполнении скрипта: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутреннее исключение: {ex.InnerException.Message}");
            }
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("Нажмите любую клавишу для выхода...");
        Console.ReadKey();
    }
    
    private static async Task<bool> CheckOrCreateAdminRole(AdminResetDbContext context)
    {
        // Проверяем существование роли Admin
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            // Создаем роль Admin
            adminRole = new IdentityRole
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            context.Roles.Add(adminRole);
            await context.SaveChangesAsync();
            Console.WriteLine("Создана роль Admin");
        }
        
        return true;
    }
}

// Классы для работы с базой данных
public class AdminResetDbContext : DbContext
{
    public AdminResetDbContext(DbContextOptions<AdminResetDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<IdentityRole> Roles { get; set; } = null!;
    public DbSet<IdentityUserRole<string>> UserRoles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Настраиваем первичный ключ для IdentityUserRole
        builder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.UserId, r.RoleId });

        // Задаем имена таблиц для Identity
        builder.Entity<User>().ToTable("AspNetUsers");
        builder.Entity<IdentityRole>().ToTable("AspNetRoles");
        builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
    }
}

public class User
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string NormalizedUserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public string ConcurrencyStamp { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    
    // Дополнительные поля
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public bool IsActive { get; set; }
}
