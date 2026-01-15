using AutoMapper;
using BLL.Interfaces;
using BLL.ModelsDto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.ViewModels;

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для управления ролями пользователей.
    /// Доступен только администраторам.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class RolesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IMapper mapper, IRoleService roleService, ILogger<RolesController> logger)
        {
            _mapper = mapper;
            _roleService = roleService;
            _logger = logger;
        }

        /// <summary>
        /// Получение списка всех ролей в системе.
        /// </summary>
        /// <returns>Список всех ролей.</returns>
        /// <response code="200">Роли успешно получены.</response>
        /// <response code="500">Ошибка при обработке запроса.</response>
        [HttpGet("All")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var roles = await _roleService.GetAllAsync();
                var viewModels = _mapper.Map<IEnumerable<RoleViewModel>>(roles);
                return Ok(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении всех ролей.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Получение роли по её идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор роли.</param>
        /// <returns>Роль, если найдена.</returns>
        /// <response code="200">Роль найдена.</response>
        /// <response code="404">Роль не найдена.</response>
        [HttpGet("by-id/{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _roleService.GetByIdAsync(id);
            if (result.Data == null)
                return NotFound();

            var viewModel = _mapper.Map<RoleViewModel>(result.Data);
            return Ok(viewModel);
        }

        /// <summary>
        /// Поиск ролей по имени.
        /// </summary>
        /// <param name="name">Имя роли или его часть.</param>
        /// <returns>Список найденных ролей.</returns>
        /// <response code="200">Роли успешно найдены.</response>
        /// <response code="404">Роли не найдены.</response>
        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetByNames(string name)
        {
            var result = await _roleService.GetByNamesAsync(name);
            if (!result.Success)
                return NotFound(result.Errors);

            var viewModel = _mapper.Map<IEnumerable<RoleViewModel>>(result.Data);
            return Ok(viewModel);
        }

        /// <summary>
        /// Создание новой роли.
        /// </summary>
        /// <param name="model">Модель с названием роли.</param>
        /// <returns>Созданная роль.</returns>
        /// <response code="201">Роль успешно создана.</response>
        /// <response code="400">Ошибка валидации модели или роль уже существует.</response>
        /// <response code="500">Ошибка при создании роли.</response>
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] RegisterRoleModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var dto = _mapper.Map<RoleDto>(model);
                var result = await _roleService.Create(dto.Name);

                if (!result.Success)
                    return BadRequest(result.Errors);

                return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании роли.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Обновление существующей роли.
        /// </summary>
        /// <param name="model">Модель роли с изменёнными данными.</param>
        /// <returns>Результат обновления роли.</returns>
        /// <response code="200">Роль успешно обновлена.</response>
        /// <response code="400">Ошибка валидации модели или некорректные данные.</response>
        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] RoleViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dto = _mapper.Map<RoleDto>(model);
            var result = await _roleService.UpdateAsync(dto);

            if (!result.Success)
                return BadRequest(result.Errors);

            return Ok();
        }

        /// <summary>
        /// Удаление роли по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор роли.</param>
        /// <returns>Результат удаления.</returns>
        /// <response code="204">Роль успешно удалена.</response>
        /// <response code="400">Ошибка при удалении роли.</response>
        /// <response code="404">Роль не найдена.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _roleService.Delete(id);

            if (!result.Success)
                return BadRequest(result.Errors);

            return NoContent();
        }
    }
}
