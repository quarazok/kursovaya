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
        var totalUsers     = await _db.Users.CountAsync();
        var totalAdmins    = await _db.Users.CountAsync(u => u.Role == UserRole.Admin);
        var totalEmployees = await _db.Users.CountAsync(u => u.Role == UserRole.Employee);
        var totalClients   = await _db.Clients.CountAsync();
        var totalOrders    = await _db.Orders.CountAsync();
        var totalCouriers  = await _db.Couriers.CountAsync();
        var totalDeliveries = await _db.Deliveries.CountAsync();
        var totalPayments  = await _db.Payments.CountAsync();
        var totalRevenue   = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Paid)
            .SumAsync(p => p.Amount);

        return Ok(new
        {
            totalUsers,
            totalAdmins,
            totalEmployees,
            totalClients,
            totalOrders,
            totalCouriers,
            totalDeliveries,
            totalPayments,
            totalRevenue,
        });
    }

    // PATCH api/users/5/role
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UserRole role)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        user.Role = role;
        await _db.SaveChangesAsync();

        return Ok(new { user.Id, user.FirstName, user.LastName, user.Email, user.Role });
    }

    // DELETE api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
