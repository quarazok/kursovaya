using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[Authorize(Roles = "Employee,Admin")]
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

    // POST api/couriers
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Courier courier)
    {
        _db.Couriers.Add(courier);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = courier.Id }, courier);
    }

    // PUT api/couriers/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Courier updated)
    {
        var courier = await _db.Couriers.FindAsync(id);

        if (courier == null)
            return NotFound();

        courier.FirstName   = updated.FirstName;
        courier.LastName    = updated.LastName;
        courier.Phone       = updated.Phone;
        courier.Email       = updated.Email;
        courier.IsAvailable = updated.IsAvailable;

        await _db.SaveChangesAsync();
        return Ok(courier);
    }

    // DELETE api/couriers/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var courier = await _db.Couriers.FindAsync(id);

        if (courier == null)
            return NotFound();

        var deliveriesCount = await _db.Deliveries.CountAsync(d => d.CourierId == id);
        if (deliveriesCount > 0)
            return BadRequest($"Нельзя удалить курьера: за ним закреплено {deliveriesCount} доставк(и). Сначала удалите доставки.");

        _db.Couriers.Remove(courier);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
