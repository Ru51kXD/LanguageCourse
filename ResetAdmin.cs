using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using WebApplication15.Data;
using WebApplication15.Models;

namespace WebApplication15
{
    public class ResetAdmin
    {
        public static async Task Reset(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Проверяем и создаем роль Admin, если её нет
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            
            // Находим пользователя admin, если он существует
            var adminUser = await userManager.FindByNameAsync("admin");
            
            // Если пользователь существует, удаляем его
            if (adminUser != null)
            {
                // Сначала удалим пользователя из ролей
                var userRoles = await userManager.GetRolesAsync(adminUser);
                await userManager.RemoveFromRolesAsync(adminUser, userRoles);
                
                // Удаляем пользователя
                await userManager.DeleteAsync(adminUser);
            }
            
            // Создаем нового администратора
            var newAdmin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                RegistrationDate = DateTime.Now,
                IsActive = true
            };
            
            // Создаем учетную запись с простым паролем
            var createResult = await userManager.CreateAsync(newAdmin, "admin123");
            
            if (createResult.Succeeded)
            {
                // Добавляем пользователя в роль Admin
                await userManager.AddToRoleAsync(newAdmin, "Admin");
                Console.WriteLine("Администратор успешно пересоздан!");
                Console.WriteLine("Логин: admin");
                Console.WriteLine("Пароль: admin123");
            }
            else
            {
                Console.WriteLine("Ошибка при создании администратора:");
                foreach (var error in createResult.Errors)
                {
                    Console.WriteLine($"- {error.Description}");
                }
            }
        }
    }
} 