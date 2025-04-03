using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using WebApplication15.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace WebApplication15.Services
{
    public class AuthService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthService(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Регистрация нового пользователя
        public async Task<(bool Success, IEnumerable<IdentityError>? Errors)> RegisterUser(ApplicationUser user, string password)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    return (false, new[] { new IdentityError { Description = "Пользователь или Email отсутствует" } });
                }

                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    return (false, new[] { new IdentityError { Description = "Пользователь с таким email уже существует" } });
                }

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return (true, null);
                }
                
                return (false, result.Errors);
            }
            catch (Exception ex)
            {
                // Можно добавить логирование ошибки
                return (false, new[] { new IdentityError { Description = $"Произошла ошибка: {ex.Message}" } });
            }
        }

        // Аутентификация пользователя
        public async Task<bool> Login(string email, string password)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return false;
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return false;
                }

                var result = await _signInManager.PasswordSignInAsync(email, password, false, false);
                return result.Succeeded;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Выход пользователя
        public async Task Logout()
        {
            await _signInManager.SignOutAsync();
        }

        // Получение текущего пользователя
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        public string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        }

        public bool IsInRole(string role)
        {
            return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
        }
        
        public string GetUserId(ClaimsPrincipal user)
        {
            return user?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        public async Task<ApplicationUser> GetCurrentUser()
        {
            if (!IsAuthenticated())
                return null;

            var userName = _httpContextAccessor.HttpContext.User.Identity.Name;
            var user = await _userManager.FindByNameAsync(userName);
            return user;
        }

        // Поиск пользователя по email
        public async Task<ApplicationUser?> FindByEmailAsync(string? email)
        {
            try 
            {
                if (string.IsNullOrEmpty(email))
                {
                    return null;
                }

                return await _userManager.FindByEmailAsync(email);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> RegisterUserAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;
                
            var user = new ApplicationUser { UserName = email, Email = email };
            var result = await _userManager.CreateAsync(user, password);
            
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return true;
            }
            
            return false;
        }

        public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;
                
            var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, false);
            return result.Succeeded;
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }

        public async Task<ApplicationUser?> GetCurrentUserAsync(ClaimsPrincipal? userPrincipal)
        {
            if (userPrincipal == null)
                return null;
                
            return await _userManager.GetUserAsync(userPrincipal);
        }
    }
} 