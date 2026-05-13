using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DeliveryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Client")]
public class ClientPortalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public ClientPortalController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // POST api/clientportal/register
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] ClientRegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            return BadRequest("Пароли не совпадают");

        if (dto.Password.Length < 6)
            return BadRequest("Пароль должен содержать не менее 6 символов");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Пользователь с таким email уже существует");

        if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone))
            return BadRequest("Пользователь с таким телефоном уже существует");

        var user = new User
        {
            FirstName    = dto.FirstName,
            LastName     = dto.LastName,
            Email        = dto.Email,
            Phone        = dto.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role         = UserRole.Client,
            CreatedAt    = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Создаём Client-запись связанную с этим User
        var client = new Client
        {
            FirstName = dto.FirstName,
            LastName  = dto.LastName,
            Phone     = dto.Phone,
            Email     = dto.Email,
            UserId    = user.Id,
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        return Ok(BuildAuthResponse(user));
    }

    // GET api/clientportal/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Phone,
            user.CreatedAt,
        });
    }

    // GET api/clientportal/orders
    [HttpGet("orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = GetUserId();
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return Ok(new List<object>());

        var orders = await _db.Orders
            .Where(o => o.ClientId == client.Id)
            .Include(o => o.Delivery).ThenInclude(d => d!.Courier)
            .Include(o => o.Payment)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.PickupAddress,
                o.DeliveryAddress,
                o.Description,
                o.Status,
                o.CreatedAt,
                Delivery = o.Delivery == null ? null : new
                {
                    o.Delivery.Status,
                    o.Delivery.AssignedAt,
                    o.Delivery.CompletedAt,
                    CourierName = o.Delivery.Courier == null
                        ? null
                        : o.Delivery.Courier.FirstName + " " + o.Delivery.Courier.LastName,
                },
                Payment = o.Payment == null ? null : new
                {
                    o.Payment.Amount,
                    o.Payment.Status,
                    o.Payment.PaymentMethod,
                    o.Payment.PaidAt,
                },
            })
            .ToListAsync();

        return Ok(orders);
    }

    // POST api/clientportal/orders
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] ClientOrderDto dto)
    {
        var userId = GetUserId();
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return BadRequest("Профиль клиента не найден");

        if (string.IsNullOrWhiteSpace(dto.PickupAddress) ||
            string.IsNullOrWhiteSpace(dto.DeliveryAddress))
            return BadRequest("Укажите адреса отправки и доставки");

        var order = new Order
        {
            ClientId        = client.Id,
            PickupAddress   = dto.PickupAddress,
            DeliveryAddress = dto.DeliveryAddress,
            Description     = dto.Description,
            CreatedAt       = DateTime.UtcNow,
            Status          = OrderStatus.New,
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new { order.Id, order.Status, order.CreatedAt });
    }

    // GET api/clientportal/orders/{id}
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> TrackOrder(int id)
    {
        var userId = GetUserId();
        var client = await _db.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
        if (client == null) return NotFound();

        var order = await _db.Orders
            .Where(o => o.Id == id && o.ClientId == client.Id)
            .Include(o => o.Delivery).ThenInclude(d => d!.Courier)
            .Include(o => o.Payment)
            .Select(o => new
            {
                o.Id,
                o.PickupAddress,
                o.DeliveryAddress,
                o.Description,
                o.Status,
                o.CreatedAt,
                Delivery = o.Delivery == null ? null : new
                {
                    o.Delivery.Status,
                    o.Delivery.AssignedAt,
                    o.Delivery.CompletedAt,
                    CourierFirstName = o.Delivery.Courier == null ? null : o.Delivery.Courier.FirstName,
                },
                Payment = o.Payment == null ? null : new
                {
                    o.Payment.Amount,
                    o.Payment.Status,
                    o.Payment.PaymentMethod,
                    o.Payment.PaidAt,
                },
            })
            .FirstOrDefaultAsync();

        if (order == null) return NotFound();
        return Ok(order);
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private object BuildAuthResponse(User user) => new
    {
        token     = GenerateToken(user),
        id        = user.Id,
        firstName = user.FirstName,
        lastName  = user.LastName,
        email     = user.Email,
        role      = user.Role.ToString(),
    };

    private string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
        };

        var token = new JwtSecurityToken(
            issuer:             _config["Jwt:Issuer"],
            audience:           _config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record ClientRegisterDto(
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    string ConfirmPassword);

public record ClientOrderDto(
    string PickupAddress,
    string DeliveryAddress,
    string? Description);
