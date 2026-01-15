using AutoMapper;
using BLL.Interfaces;
using BLL.ModelsDto;
using DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.ViewModels.Tags;

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для управления тегами статей.
    /// Поддерживает создание, получение, обновление и удаление тегов.
    /// </summary>
    [ApiController, Authorize]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;
        private readonly IMapper _mapper;
        private readonly IService<Tag, TagDto> _Service;

        public TagController(ITagService tagService, IMapper mapper, IService<Tag, TagDto> service)
        {
            _tagService = tagService;
            _mapper = mapper;
            _Service = service;
        }

        #region Create Tag

        /// <summary>
        /// Создание нового тега.
        /// </summary>
        /// <param name="model">Модель с данными тега.</param>
        /// <returns>Созданный тег.</returns>
        /// <response code="201">Тег успешно создан.</response>
        /// <response code="400">Ошибка валидации модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="409">Тег с таким названием уже существует.</response>
        [HttpPost("Create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] RegisterTagModel model)
        {
            var dto = _mapper.Map<TagDto>(model);
            var result = await _tagService.CreateAsync(dto, User);
            return StatusCode(result.StatusCode, result);
        }

        #endregion Create Tag

        #region Find Tag

        /// <summary>
        /// Поиск тега по имени.
        /// </summary>
        /// <param name="name">Название тега или его часть.</param>
        /// <returns>Список тегов, соответствующих запросу.</returns>
        /// <response code="200">Теги успешно найдены.</response>
        /// <response code="404">Теги не найдены.</response>
        [HttpGet("by-name/")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FindByName(string? name)
        {
            var result = await _tagService.FindByNameAsync(name);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Поиск тега по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор тега.</param>
        /// <returns>Информация о теге.</returns>
        /// <response code="200">Тег найден.</response>
        /// <response code="404">Тег не найден.</response>
        [HttpGet("by-id/{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> FindById(Guid id)
        {
            var result = await _tagService.FindByIdAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Получение списка всех тегов.
        /// </summary>
        /// <returns>Список всех доступных тегов.</returns>
        /// <response code="200">Теги успешно получены.</response>
        /// <response code="204">Нет данных.</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _Service.GetAllAsync();
            return Ok(result);
        }

        #endregion Find Tag

        #region Update Tag

        /// <summary>
        /// Обновление существующего тега.
        /// </summary>
        /// <param name="model">Модель с обновлёнными данными тега.</param>
        /// <returns>Обновлённый тег.</returns>
        /// <response code="200">Тег успешно обновлён.</response>
        /// <response code="400">Ошибка валидации модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Тег не найден.</response>
        [HttpPut("update")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Update([FromBody] UpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var dto = _mapper.Map<TagDto>(model);
                var result = await _tagService.UpdateAsync(dto);

                return StatusCode(result.StatusCode, result?.Data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex?.InnerException?.Message ?? ex.Message);
            }
        }

        #endregion Update Tag

        #region Delete Tag

        /// <summary>
        /// Удаление тега по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор тега.</param>
        /// <returns>Результат операции удаления.</returns>
        /// <response code="204">Тег успешно удалён.</response>
        /// <response code="400">Ошибка при удалении тега.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Тег не найден.</response>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _tagService.DeleteAsync(id);
            return StatusCode(result.StatusCode, result);
        }

        #endregion Delete Tag
    }
}
