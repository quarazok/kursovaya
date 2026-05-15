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

    // Тестовые аккаунты для демонстрации. Идемпотентно: повторный запуск ничего не дублирует.
    await SeedTestUserAsync(db, "test.employee@delivery.local", "Тест", "Сотрудник",
                            "+70000000001", UserRole.Employee);
    await SeedTestUserAsync(db, "test.admin@delivery.local", "Тест", "Админ",
                            "+70000000002", UserRole.Admin);
    await SeedTestClientAsync(db, "test.client@delivery.local", "Тест", "Клиент",
                              "+70000000003");
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
