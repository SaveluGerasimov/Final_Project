using AutoMapper;
using BLL;
using BLL.Interfaces;
using BLL.ModelsDto;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApi.ViewModels.Articles;

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для управления статьями: создание, поиск, редактирование и удаление.
    /// </summary>
    [ApiController, Authorize]
    [Route("api/[controller]")]
    public class ArticleController : ControllerBase
    {
        private readonly IArticleService _articleService;
        private readonly ILogger<ArticleController> _logger;
        private readonly IMapper _mapper;

        public ArticleController(IArticleService articleService, IMapper mapper, ILogger<ArticleController> logger)
        {
            _articleService = articleService;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// Создаёт новую статью.
        /// </summary>
        /// <param name="model">Модель с данными статьи: заголовок, содержание и теги.</param>
        /// <returns>Возвращает результат создания статьи.</returns>
        /// <response code="200">Статья успешно создана.</response>
        /// <response code="400">Переданы некорректные данные модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateArticleViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dto = _mapper.Map<ArticleDto>(model);
            dto.AuthorId = User.Identity.GetUserId();
            var result = await _articleService.CreateAsync(dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Создаёт новую статью (альтернативная версия метода).
        /// </summary>
        /// <param name="model">Модель с данными статьи: заголовок, содержание и теги.</param>
        /// <returns>Возвращает результат создания статьи.</returns>
        /// <response code="200">Статья успешно создана.</response>
        /// <response code="400">Переданы некорректные данные модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost("Create2")]
        public async Task<IActionResult> Create2([FromBody] CreateArticleViewModel model)
        {
            var dto = _mapper.Map<ArticleDto>(model);
            dto.AuthorId = User.Identity.GetUserId();
            var result = await _articleService.CreateAsync2(dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// Выполняет поиск статьи по заголовку.
        /// </summary>
        /// <param name="title">Заголовок статьи (или его часть).</param>
        /// <returns>Возвращает найденные статьи, соответствующие запросу.</returns>
        /// <response code="200">Запрос успешно выполнен.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpGet]
        public async Task<IActionResult> FindByTitle(string? title)
        {
            var res = await _articleService.FindByTitleAsync(title);
            return StatusCode(res.StatusCode, res?.Data);
        }

        /// <summary>
        /// Возвращает список статей, начиная с указанного индекса.
        /// </summary>
        /// <param name="startIndex">Индекс первой статьи, с которой начинается выборка.</param>
        /// <param name="count">Количество статей, которое нужно получить.</param>
        /// <returns>Список статей, начиная с указанного индекса.</returns>
        /// <response code="200">Запрос успешно выполнен. Возвращён список статей.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Статьи не найдены.</response>
        [HttpGet("{startIndex}/{count}")]
        public async Task<IActionResult> Get(int startIndex = 0, int count = 10)
        {
            (int startIndex, int count) item;
            item.startIndex = startIndex;
            item.count = count;

            var res = await _articleService.GetLatestArticlesAsync(item);
            return StatusCode(res.StatusCode, res.Data);
        }

        /// <summary>
        /// Возвращает статьи, созданные определённым автором.
        /// </summary>
        /// <param name="authorId">Идентификатор автора.</param>
        /// <returns>Список статей автора.</returns>
        /// <response code="200">Запрос успешно выполнен.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">У автора нет статей.</response>
        [HttpGet("author/{authorId}")]
        public async Task<IActionResult> GetByAuthor(string authorId)
        {
            var result = await _articleService.GetByAuthorIdAsync(authorId);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result.Data);
        }

        /// <summary>
        /// Возвращает статью по её идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор статьи.</param>
        /// <returns>Данные найденной статьи.</returns>
        /// <response code="200">Статья успешно найдена.</response>
        /// <response code="400">Указан недопустимый идентификатор.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Статья не найдена.</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> FindById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid ID");

            try
            {
                var result = await _articleService.FindByIdAsync(id);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding article by id {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Удаляет статью по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор статьи для удаления.</param>
        /// <returns>Результат удаления статьи.</returns>
        /// <response code="200">Статья успешно удалена.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="403">Недостаточно прав для удаления статьи.</response>
        /// <response code="404">Статья не найдена.</response>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return Unauthorized();

            try
            {
                var result = await _articleService.DeleteAsync(id, currentUserId);

                if (result.Errors != null)
                    return StatusCode(result.StatusCode, result.Errors);

                return StatusCode(result.StatusCode, result?.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting article {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Редактирует существующую статью.
        /// </summary>
        /// <param name="model">Модель с изменёнными данными статьи.</param>
        /// <returns>Обновлённая статья.</returns>
        /// <response code="200">Статья успешно обновлена.</response>
        /// <response code="400">Ошибки валидации модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="404">Статья не найдена.</response>
        [HttpPut("Edit")]
        public async Task<IActionResult> Edit([FromBody] EditArticleViewModel model)
        {
            var editorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(editorId))
                return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Ошибки валидации",
                    Errors = ModelState
                        .SelectMany(ms => ms.Value.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }

            var dto = _mapper.Map<ArticleDto>(model);
            var result = await _articleService.Update(dto, editorId);

            if (!result.Success)
                return StatusCode(result.StatusCode, result.Errors);

            return StatusCode(result.StatusCode, result.Data);
        }
    }
}
