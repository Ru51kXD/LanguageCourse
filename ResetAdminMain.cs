using System;
using System.Threading.Tasks;

namespace WebApplication15
{
    class ResetAdminMain
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск скрипта сброса пароля администратора...");
            
            try
            {
                await ResetAdminScript.Execute();
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
    }
} 