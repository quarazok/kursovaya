using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "DeliveryApi v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Автоматически применяем миграции при старте с retry —
// БД в Docker может прогреваться 10-30 секунд при первом запуске
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    for (int i = 0; i < 10; i++)
    {
        try
        {
            db.Database.Migrate();
            break;
        }
        catch when (i < 9)
        {
            await Task.Delay(3000);
        }
    }

    // Миграция данных: после удаления роли Employee (0) переводим всех старых
    // сотрудников в Operator, иначе их роль не пройдёт авторизацию.
    var legacy = await db.Users.Where(u => (int)u.Role == 0).ToListAsync();
    if (legacy.Count > 0)
    {
        foreach (var u in legacy) u.Role = UserRole.Operator;
        await db.SaveChangesAsync();
    }

    // Тестовые аккаунты для демонстрации. Идемпотентно: повторный запуск ничего не дублирует.
    await SeedTestUserAsync(db, "test.admin@delivery.local",      "Тест", "Админ",
                            "+70000000001", UserRole.Admin);
    await SeedTestUserAsync(db, "test.operator@delivery.local",   "Тест", "Оператор",
                            "+70000000002", UserRole.Operator);
    await SeedTestUserAsync(db, "test.courier@delivery.local",    "Тест", "Курьер",
                            "+70000000003", UserRole.Courier);
    await SeedTestUserAsync(db, "test.accountant@delivery.local", "Тест", "Бухгалтер",
                            "+70000000004", UserRole.Accountant);
    await SeedTestClientAsync(db, "test.client@delivery.local",   "Тест", "Клиент",
                              "+70000000005");

    // Несколько готовых курьеров и клиентов, чтобы оператор сразу видел данные.
    await SeedSampleCouriersAsync(db);
    await SeedSampleClientsAsync(db);
}

app.Run();

static async Task SeedTestUserAsync(AppDbContext db, string email, string firstName,
                                    string lastName, string phone, UserRole role)
{
    if (await db.Users.AnyAsync(u => u.Email == email)) return;

    db.Users.Add(new User
    {
        FirstName    = firstName,
        LastName     = lastName,
        Email        = email,
        Phone        = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234"),
        Role         = role,
        CreatedAt    = DateTime.UtcNow,
    });
    await db.SaveChangesAsync();
}

// Идемпотентный seed: проверяем по email, чтобы не дублировать при повторных запусках.
static async Task SeedSampleCouriersAsync(AppDbContext db)
{
    var samples = new[]
    {
        // Курьер для тест-аккаунта test.courier@delivery.local — email совпадает
        // с пользователем, чтобы серверная проверка «своей доставки» сработала.
        new Courier { FirstName = "Тест",     LastName = "Курьер",   Phone = "+70000000003", Email = "test.courier@delivery.local", IsAvailable = true  },
        new Courier { FirstName = "Алексей",  LastName = "Смирнов",  Phone = "+79001110001", Email = "smirnov@delivery.local",      IsAvailable = true  },
        new Courier { FirstName = "Дмитрий",  LastName = "Иванов",   Phone = "+79001110002", Email = "ivanov@delivery.local",       IsAvailable = true  },
        new Courier { FirstName = "Сергей",   LastName = "Кузнецов", Phone = "+79001110003", Email = "kuznetsov@delivery.local",    IsAvailable = false },
    };

    foreach (var c in samples)
    {
        if (!await db.Couriers.AnyAsync(x => x.Email == c.Email))
            db.Couriers.Add(c);
    }
    await db.SaveChangesAsync();
}

static async Task SeedSampleClientsAsync(AppDbContext db)
{
    var samples = new[]
    {
        new Client { FirstName = "Иван",    LastName = "Петров",    Phone = "+79002220001", Email = "petrov@example.com"    },
        new Client { FirstName = "Мария",   LastName = "Соколова",  Phone = "+79002220002", Email = "sokolova@example.com"  },
        new Client { FirstName = "ООО",     LastName = "ТехноСтрой", Phone = "+79002220003", Email = "tehno@example.com"     },
    };

    foreach (var c in samples)
    {
        if (!await db.Clients.AnyAsync(x => x.Email == c.Email))
            db.Clients.Add(c);
    }
    await db.SaveChangesAsync();
}

static async Task SeedTestClientAsync(AppDbContext db, string email, string firstName,
                                      string lastName, string phone)
{
    if (await db.Users.AnyAsync(u => u.Email == email)) return;

    var user = new User
    {
        FirstName    = firstName,
        LastName     = lastName,
        Email        = email,
        Phone        = phone,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234"),
        Role         = UserRole.Client,
        CreatedAt    = DateTime.UtcNow,
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    db.Clients.Add(new Client
    {
        FirstName = firstName,
        LastName  = lastName,
        Phone     = phone,
        Email     = email,
        UserId    = user.Id,
    });
    await db.SaveChangesAsync();
}
