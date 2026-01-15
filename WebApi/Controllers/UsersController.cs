using AutoMapper;
using BLL.Interfaces;
using BLL.ModelsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.ViewModels;

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для управления пользователями.
    /// Содержит операции для создания, получения, редактирования и удаления пользователей.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public UsersController(IUserService userService, IMapper mapper)
        {
            _userService = userService;
            _mapper = mapper;
        }

        /// <summary>
        /// Получить список всех пользователей.
        /// </summary>
        /// <returns>Список пользователей.</returns>
        /// <response code="200">Пользователи успешно получены.</response>
        /// <response code="404">Пользователи не найдены.</response>
        [AllowAnonymous]
        [HttpGet("All")]
        [ProducesResponseType(typeof(IEnumerable<UserViewModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var usersDto = await _userService.GetAllUsersAsync();
            if (usersDto == null)
                return NotFound();

            return Ok(_mapper.Map<IEnumerable<UserViewModel>>(usersDto));
        }

        /// <summary>
        /// Получить пользователя по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор пользователя.</param>
        /// <returns>Информация о пользователе.</returns>
        /// <response code="200">Пользователь найден.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(_mapper.Map<UserViewModel>(user));
        }

        /// <summary>
        /// Создать нового пользователя.
        /// </summary>
        /// <param name="model">Данные нового пользователя.</param>
        /// <returns>Созданный пользователь.</returns>
        /// <response code="201">Пользователь успешно создан.</response>
        /// <response code="400">Ошибка валидации модели или некорректные данные.</response>
        [AllowAnonymous]
        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] RegisterUserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userDto = _mapper.Map<UserDto>(model);
            var result = await _userService.CreateUserAsync(userDto);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            var createdUser = result.Data!;
            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Создать администратора по умолчанию (только для инициализации системы).
        /// </summary>
        /// <returns>Данные созданного администратора (email и пароль).</returns>
        /// <response code="201">Администратор успешно создан.</response>
        /// <response code="400">Ошибка при создании администратора.</response>
        [AllowAnonymous]
        [HttpPost("CreateAdministrator")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAdmin()
        {
            var defaultpass = "12345678";
            var model = new RegisterUserModel
            {
                Email = "admin@e.ru",
                UserName = "admin",
                Password = defaultpass
            };

            var userDto = _mapper.Map<UserDto>(model);
            var createAdmin = await _userService.CreateUserAsync(userDto);

            if (!createAdmin.DataIsNull)
            {
                var editRoleResult = await _userService.EditUserRoleAsync(createAdmin.Data.Id, "Administrator");
                if (!editRoleResult.Success)
                    return StatusCode(editRoleResult.StatusCode, string.Join("\r\n", editRoleResult.Errors));
            }

            if (!createAdmin.Success)
                return StatusCode(createAdmin.StatusCode, string.Join("\r\n", createAdmin.Errors));

            return Ok(new { model.Email, model.Password });
        }

        /// <summary>
        /// Удалить пользователя по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор пользователя.</param>
        /// <returns>Результат операции удаления.</returns>
        /// <response code="200">Пользователь успешно удалён.</response>
        /// <response code="400">Ошибка при удалении пользователя.</response>
        /// <response code="401">Нет прав доступа.</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Id не может быть пустым.");

            var result = await _userService.DeleteUserAsync(id);

            return result.Success
                ? Ok("Пользователь успешно удалён.")
                : BadRequest(result.Errors);
        }

        /// <summary>
        /// Получить информацию о текущем авторизованном пользователе.
        /// </summary>
        /// <returns>Данные текущего пользователя.</returns>
        /// <response code="200">Пользователь найден.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("Не удалось определить пользователя.");

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(_mapper.Map<UserViewModel>(user));
        }

        /// <summary>
        /// Изменить роль пользователя.
        /// </summary>
        /// <param name="model">Модель с идентификатором пользователя и новой ролью.</param>
        /// <returns>Результат обновления роли.</returns>
        /// <response code="200">Роль успешно изменена.</response>
        /// <response code="400">Ошибка при изменении роли.</response>
        [HttpPut("EditUserRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EditUserRole([FromBody] EditUserRoleViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.EditUserRoleAsync(model.UserId, model.NewRole);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result.Data);
        }

        /// <summary>
        /// Редактирование данных пользователя.
        /// </summary>
        /// <param name="id">Идентификатор пользователя.</param>
        /// <param name="model">Обновлённые данные пользователя.</param>
        /// <returns>Обновлённый пользователь.</returns>
        /// <response code="200">Пользователь успешно обновлён.</response>
        /// <response code="400">Ошибка валидации или редактирования.</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Edit(string id, [FromBody] UserViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _userService.EditUserAsync(id, _mapper.Map<UserDto>(model));

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result.Data);
        }
    }
}
