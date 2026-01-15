using BLL.Extensions;
using BLL.Interfaces;
using BLL.Models;
using BLL.Services;
using DAL;
using DAL.Interfaces;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using WebApi.Helpers;
using WebApi.Mapper;

var builder = WebApplication.CreateBuilder(args);

// Добавляем контроллеры в контейнер зависимостей (чтобы можно было использовать контроллеры и маршруты)
builder.Services.AddControllers();

// Добавляем поддержку endpoints explorer (нужно для генерации описания API для Swagger)
builder.Services.AddEndpointsApiExplorer();

// Регистрируем Swagger генератор (для документации и тестирования API через веб-интерфейс)
builder.Services.AddSwaggerGen(options =>
{
    // Основная информация о API
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WebApi Documentation",
        Version = "v1",
        Description = "Документация API для проекта WebApi",
        Contact = new OpenApiContact
        {
            Name = "NoNet",
            Email = "NoNet@example.com"
        }
    });

    // Подключение XML-комментариев (для генерации описаний из /// summary)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

   /* //Поддержка авторизации через Cookie (ASP.NET Core Identity)
    options.AddSecurityDefinition("cookieAuth", new OpenApiSecurityScheme
    {
        Name = "Cookie",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Аутентификация с помощью cookie ASP.NET Core Identity"
    });*/

    // Требование безопасности для Swagger UI
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "cookieAuth"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------- Добавленные сервисы ----------------

// Регистрируем AutoMapper с указанными профилями маппинга (BLL и WebApi слой)
builder.Services.AddAutoMapper(
    typeof(BLL.Mapper.BLLMappingProfile),
    typeof(PLLMappingProfile)
);

// Получаем строку подключения из конфигурации (appsettings.json)
string connection = builder.Configuration.GetConnectionString("DefaultConnection")
                   ?? throw new ArgumentNullException("no string connection");

// Регистрируем контекст базы данных с использованием SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connection));

// Конфигурируем Identity для пользователей (User) и ролей (IdentityRole)
builder.Services.AddIdentity<DAL.Entities.User, IdentityRole>(opts =>
{
    opts.Password.RequiredLength = 2;              // Минимальная длина пароля
    opts.Password.RequireNonAlphanumeric = false;  // Не требует спецсимволов
    opts.Password.RequireLowercase = false;        // Не требует строчных букв
    opts.Password.RequireUppercase = false;        // Не требует заглавных букв
    opts.Password.RequireDigit = false;            // Не требует цифр
})
.AddEntityFrameworkStores<AppDbContext>()         // Хранение данных Identity в EF Core
.AddDefaultTokenProviders();                      // Добавляет поддержку токенов (например, для сброса пароля)

// Регистрируем сервис для работы с ролями, добавляем дефолтные роли (например, User, Administrator)
builder.Services.AddScoped<IRoleService, RoleService>()
    .AddIdentityRoles(RoleType.User, RoleType.Administrator, RoleType.Moderator);

// Регистрируем сервис для работы с пользователями, добавляем дефолтную роль User
builder.Services.AddScoped<IUserService, UserService>()
    .AddDefaultUserRole(RoleType.User);

builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IArticleTagService, ArticleTagService>();
builder.Services.AddScoped<ICommentService, CommentService>();

// Регистрируем обобщенный репозиторий (generic repository) для работы с сущностями
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IService<,>), typeof(Service<,>));

// Настройка cookie-аутентификации
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/api/auth/login";   // URL для логина
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Настройка авторизации
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Пользовательский обработчик результатов авторизации (для кастомных ответов)
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationHandler>();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero; // Проверять каждый запрос
});

// ---------------- Конец добавленных сервисов ----------------

var app = builder.Build();

// Конфигурируем middleware (конвейер запросов)

// Включаем Swagger (в Dev и Staging средах)
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    app.UseSwagger();    // Генерация swagger.json
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "WebApi v1");
        options.RoutePrefix = "swagger"; // Swagger доступен по адресу /swagger
    });
}

// Принудительная переадресация HTTP -> HTTPS
app.UseHttpsRedirection();

app.UseAuthentication();

// Включаем middleware для авторизации (проверка прав доступа на основе атрибута [Authorize])
app.UseAuthorization();

// Подключаем маршрутизацию контроллеров
app.MapControllers();

// Запускаем приложение
app.Run();
