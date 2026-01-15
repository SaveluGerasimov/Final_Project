using AutoMapper;
using BLL;
using BLL.Interfaces;
using BLL.ModelsDto;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApi.ViewModels.Comments;

namespace WebApi.Controllers
{
    /// <summary>
    /// Контроллер для управления комментариями.
    /// Содержит методы для создания, получения, редактирования и удаления комментариев.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CommentController : ControllerBase
    {
        private readonly ICommentService _commentService;
        private readonly IMapper _mapper;

        public CommentController(ICommentService commentService, IMapper mapper)
        {
            _commentService = commentService;
            _mapper = mapper;
        }

        /// <summary>
        /// Тестовый метод для демонстрации примера ответа.
        /// </summary>
        /// <param name="model">Модель, содержащая идентификатор статьи.</param>
        /// <returns>Пример комментария, возвращаемый в стандартной структуре данных.</returns>
        /// <response code="200">Успешный тестовый ответ.</response>
        [HttpPost("Example")]
        public IActionResult TestResponse([FromBody] GetCommentViewModel model)
        {
            var cmnt = new Comment()
            {
                Id = Guid.NewGuid(),
                Message = "Тестовый ответ",
                Author = "Иван Иванов",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var vm = new CommentViewModel()
            {
                ArticleId = model.ArticleId,
                Comments = [cmnt]
            };

            return StatusCode(200, vm);
        }

        /// <summary>
        /// Добавляет новый комментарий к статье.
        /// </summary>
        /// <param name="model">Модель с данными нового комментария (текст и идентификатор статьи).</param>
        /// <returns>Созданный комментарий.</returns>
        /// <response code="200">Комментарий успешно создан.</response>
        /// <response code="400">Ошибка валидации модели.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        [HttpPost("Add")]
        public async Task<IActionResult> Add([FromBody] CreateCommentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User?.Identity?.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User is not authenticated.");

            var dto = new CommentDto
            {
                ArticleId = model.ArticleId,
                Message = model.Message,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _commentService.CreateAsync(dto);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            return StatusCode(result.StatusCode, result.Data);
        }

        /// <summary>
        /// Получает список комментариев для указанной статьи.
        /// </summary>
        /// <param name="model">Модель, содержащая идентификатор статьи и количество комментариев.</param>
        /// <returns>Список комментариев к статье.</returns>
        /// <response code="200">Комментарии успешно получены.</response>
        /// <response code="204">Комментариев нет.</response>
        /// <response code="400">Ошибка валидации модели.</response>
        [HttpPost("Get")]
        public async Task<IActionResult> Get([FromBody] GetCommentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _commentService.GetAsync(model.ArticleId, model.Count);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            if (result.DataIsNull)
                return NoContent();

            var artId = result.Data!.FirstOrDefault()?.ArticleId ?? model.ArticleId;

            var comments = new CommentViewModel
            {
                ArticleId = artId,
                Comments = _mapper.Map<List<ViewModels.Comments.Comment>>(result.Data)
            };

            return StatusCode(result.StatusCode, comments);
        }

        /// <summary>
        /// Получает комментарий по его идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор комментария.</param>
        /// <returns>Комментарий, если найден.</returns>
        /// <response code="200">Комментарий найден и возвращён.</response>
        /// <response code="400">Ошибка валидации или неверный идентификатор.</response>
        /// <response code="404">Комментарий не найден.</response>
        [HttpPost("GetById")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _commentService.GetByIdAsync(id);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\r\n", result.Errors));

            var comments = new CommentViewModel()
            {
                ArticleId = result.Data.ArticleId,
                Comments = new List<ViewModels.Comments.Comment>
                {
                    _mapper.Map<ViewModels.Comments.Comment>(result.Data)
                }
            };

            return StatusCode(result.StatusCode, comments);
        }

        /// <summary>
        /// Редактирует существующий комментарий.
        /// </summary>
        /// <param name="model">Модель с идентификатором и новым текстом комментария.</param>
        /// <returns>Обновлённый комментарий.</returns>
        /// <response code="200">Комментарий успешно обновлён.</response>
        /// <response code="400">Ошибка валидации модели или отсутствует Id.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="403">Недостаточно прав для редактирования комментария.</response>
        /// <response code="404">Комментарий не найден.</response>
        [HttpPut("Edit")]
        public async Task<IActionResult> Edit([FromBody] EditCommentViewModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (model.CommentId == default)
                return BadRequest("Id is required");

            var userId = User?.Identity?.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var dto = new CommentDto
            {
                Id = model.CommentId,
                Message = model.Message,
                AuthorId = userId,
                UpdatedAt = DateTime.UtcNow
            };

            var isPermissionEdit = User.IsInRole("Administrator") || User.IsInRole("Moderator");
            var result = await _commentService.UpdateAsync(dto, isPermissionEdit);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\n", result.Errors));

            return Ok(result);
        }

        /// <summary>
        /// Удаляет комментарий по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор комментария для удаления.</param>
        /// <returns>Сообщение о результате удаления.</returns>
        /// <response code="200">Комментарий успешно удалён.</response>
        /// <response code="400">Некорректный идентификатор.</response>
        /// <response code="401">Пользователь не авторизован.</response>
        /// <response code="403">Недостаточно прав для удаления комментария.</response>
        /// <response code="404">Комментарий не найден.</response>
        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == default)
                return BadRequest("Id is required.");

            var userId = User?.Identity?.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            // Проверка, является ли пользователь администратором
            var isAdmin = User.IsInRole("Administrator");

            var result = await _commentService.DeleteAsync(id, userId, isAdmin);

            if (!result.Success)
                return StatusCode(result.StatusCode, string.Join("\n", result.Errors));

            return Ok("Комментарий удалён.");
        }
    }
}
