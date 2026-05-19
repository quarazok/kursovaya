using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[Authorize(Roles = Roles.AllStaff)]
[ApiController]
[Route("api/[controller]")]
public class CouriersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CouriersController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/couriers
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var couriers = await _db.Couriers.ToListAsync();
        return Ok(couriers);
    }

    // GET api/couriers/available
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable()
    {
        var couriers = await _db.Couriers
            .Where(c => c.IsAvailable)
            .ToListAsync();
        return Ok(couriers);
    }

    // GET api/couriers/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var courier = await _db.Couriers.FindAsync(id);

        if (courier == null)
            return NotFound();

        return Ok(courier);
    }

    // Создание/изменение/удаление курьеров отключено: справочник read-only,
    // данные поставляются seed-ом при старте.
}
