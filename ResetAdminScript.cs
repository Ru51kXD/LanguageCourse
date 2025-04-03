using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication15.Data;
using WebApplication15.Models;

namespace WebApplication15
{
    public class ResetAdminScript
    {
        public static async Task Execute()
        {
            // Путь к базе данных сайта
            string connectionString = "Server=(localdb)\\mssqllocaldb;Database=LanguageCourses_New;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
            
            // Создаем опции для контекста
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            // Создаем контекст
            using (var context = new ApplicationDbContext(optionsBuilder.Options))
            {
                // Создаем менеджер паролей
                var passwordHasher = new PasswordHasher<ApplicationUser>();
                
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
                    
                    Console.WriteLine("Пароль успешно сброшен. Используйте логин: admin, пароль: admin123");
                }
                else
                {
                    Console.WriteLine("Пользователь admin не найден. Создаем нового...");
                    
                    // Создаем нового пользователя
                    var newAdmin = new ApplicationUser
                    {
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
                    var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                    if (adminRole == null)
                    {
                        // Создаем роль Admin
                        adminRole = new IdentityRole
                        {
                            Name = "Admin",
                            NormalizedName = "ADMIN",
                            ConcurrencyStamp = Guid.NewGuid().ToString()
                        };
                        
                        context.Roles.Add(adminRole);
                        await context.SaveChangesAsync();
                    }
                    
                    // Добавляем пользователя в роль Admin
                    context.UserRoles.Add(new IdentityUserRole<string>
                    {
                        UserId = newAdmin.Id,
                        RoleId = adminRole.Id
                    });
                    
                    await context.SaveChangesAsync();
                    
                    Console.WriteLine("Администратор успешно создан. Используйте логин: admin, пароль: admin123");
                }
            }
        }
    }
} 