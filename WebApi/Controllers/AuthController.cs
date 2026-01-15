using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApi.ViewModels; 

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для аутентификации пользователей.
    /// Позволяет выполнять вход, выход и получать информацию о профиле текущего пользователя.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Выполняет вход пользователя по email и паролю.
        /// </summary>
        /// <param name="model">Модель с email и паролем.</param>
        /// <returns>
        /// Возвращает данные пользователя и его роль при успешной аутентификации.
        /// </returns>
        /// <response code="200">Успешная аутентификация.</response>
        /// <response code="400">Некорректные данные модели.</response>
        /// <response code="401">Неверный email или пароль.</response>
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized("Invalid email or password");

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: true,
                lockoutOnFailure: false
            );
           
            if (result.Succeeded)
            {
                var role = await _userManager.GetRolesAsync(user);

                return Ok(new
                {
                     user.UserName, 
                     user.Email,
                     user.Id,
                     Role = role.FirstOrDefault()

            });
            }

            if (result.IsLockedOut)
                return Unauthorized("User is locked out");

            return Unauthorized("Invalid email or password");
        }

        /// <summary>
        /// Выполняет выход текущего авторизованного пользователя.
        /// </summary>
        /// <returns>Статус успешного выхода.</returns>
        /// <response code="200">Пользователь успешно вышел из системы.</response>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok("Logged out successfully");
        }

        /// <summary>
        /// Проверка авторизованного пользователя.
        /// </summary>
        /// <returns>
        /// Возвращает данные пользователя UserName и Email.
        /// </returns>
        /// <response code="200">Успешная проверка авторизации.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Пользователь не найден.</response>
        [Authorize, HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound("User not found");

            return Ok(new
            {
                user.UserName,
                user.Email
            });
        }
    }
}