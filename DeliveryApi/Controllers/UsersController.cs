using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Phone,
                u.Role,
                u.CreatedAt,
            })
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        return Ok(users);
    }

    // GET api/users/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers       = await _db.Users.CountAsync();
        var totalAdmins      = await _db.Users.CountAsync(u => u.Role == UserRole.Admin);
        var totalOperators   = await _db.Users.CountAsync(u => u.Role == UserRole.Operator);
        var totalCouriersUsr = await _db.Users.CountAsync(u => u.Role == UserRole.Courier);
        var totalAccountants = await _db.Users.CountAsync(u => u.Role == UserRole.Accountant);
        var totalStaff       = totalAdmins + totalOperators + totalCouriersUsr + totalAccountants;
        var totalClients     = await _db.Clients.CountAsync();
        var totalOrders      = await _db.Orders.CountAsync();
        var totalCouriers    = await _db.Couriers.CountAsync();
        var totalDeliveries  = await _db.Deliveries.CountAsync();
        var totalPayments    = await _db.Payments.CountAsync();
        var totalRevenue     = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount);

        return Ok(new
        {
            totalUsers,
            totalStaff,
            totalAdmins,
            totalOperators,
            totalCouriersUsr,
            totalAccountants,
            totalClients,
            totalOrders,
            totalCouriers,
            totalDeliveries,
            totalPayments,
            totalRevenue,
        });
    }

    // Изменение/удаление пользователей отключено: справочник read-only.
}
