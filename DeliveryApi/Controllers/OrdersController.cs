using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/orders
    // GET api/orders?clientId=5
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? clientId)
    {
        var query = _db.Orders
            .Include(o => o.Client)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(o => o.ClientId == clientId.Value);

        var orders = await query.ToListAsync();
        return Ok(orders);
    }

    // GET api/orders/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    // POST api/orders
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Order order)
    {
        order.CreatedAt = DateTime.UtcNow;
        order.Status = OrderStatus.New;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    // PUT api/orders/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Order updated)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        order.ClientId       = updated.ClientId;
        order.PickupAddress  = updated.PickupAddress;
        order.DeliveryAddress = updated.DeliveryAddress;
        order.Description    = updated.Description;

        await _db.SaveChangesAsync();
        return Ok(order);
    }

    // PATCH api/orders/5/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        order.Status = status;
        await _db.SaveChangesAsync();
        return Ok(order);
    }

    // DELETE api/orders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound();

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
