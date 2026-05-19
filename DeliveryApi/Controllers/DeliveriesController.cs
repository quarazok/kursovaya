using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DeliveryApi.Controllers;

[Authorize(Roles = Roles.AllStaff)]
[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DeliveriesController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/deliveries
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var deliveries = await _db.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .ToListAsync();
        return Ok(deliveries);
    }

    // GET api/deliveries/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
            return NotFound();

        return Ok(delivery);
    }

    // POST api/deliveries — назначение делает оператор/админ
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrOperator)]
    public async Task<IActionResult> Create([FromBody] Delivery delivery)
    {
        // Проверяем что заказ существует
        var order = await _db.Orders.FindAsync(delivery.OrderId);
        if (order == null)
            return BadRequest("Заказ не найден");

        // Проверяем что курьер существует и свободен
        var courier = await _db.Couriers.FindAsync(delivery.CourierId);
        if (courier == null)
            return BadRequest("Курьер не найден");
        if (!courier.IsAvailable)
            return BadRequest("Курьер занят");

        delivery.AssignedAt = DateTime.UtcNow;
        delivery.Status = DeliveryStatus.Assigned;

        // Помечаем курьера как занятого и обновляем статус заказа
        courier.IsAvailable = false;
        order.Status = OrderStatus.Assigned;

        _db.Deliveries.Add(delivery);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);
    }

    // PUT api/deliveries/5
    [HttpPut("{id}")]
    [Authorize(Roles = Roles.Dispatch)]
    public async Task<IActionResult> Update(int id, [FromBody] Delivery updated)
    {
        var delivery = await _db.Deliveries.FindAsync(id);

        if (delivery == null)
            return NotFound();

        delivery.Status = updated.Status;
        delivery.CourierId = updated.CourierId;

        await _db.SaveChangesAsync();
        return Ok(delivery);
    }

    // POST api/deliveries/5/complete — курьер завершает доставку
    [HttpPost("{id}/complete")]
    [Authorize(Roles = Roles.Dispatch)]
    public async Task<IActionResult> Complete(int id)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Order)
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
            return NotFound();

        // Курьер может завершать только свою доставку.
        // Связь User <-> Courier — по совпадению email (см. seed).
        if (User.IsInRole(nameof(UserRole.Courier)))
        {
            var myEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(myEmail) ||
                !string.Equals(delivery.Courier?.Email, myEmail, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Нельзя завершить чужую доставку");
            }
        }

        if (delivery.Status == DeliveryStatus.Completed)
            return BadRequest("Доставка уже завершена");

        delivery.Status = DeliveryStatus.Completed;
        delivery.CompletedAt = DateTime.UtcNow;

        // Освобождаем курьера
        if (delivery.Courier != null)
            delivery.Courier.IsAvailable = true;

        // Обновляем статус заказа
        if (delivery.Order != null)
            delivery.Order.Status = OrderStatus.Delivered;

        await _db.SaveChangesAsync();
        return Ok(delivery);
    }

    // DELETE api/deliveries/5
    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.AdminOrOperator)]
    public async Task<IActionResult> Delete(int id)
    {
        var delivery = await _db.Deliveries
            .Include(d => d.Courier)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (delivery == null)
            return NotFound();

        // Освобождаем курьера при удалении доставки
        if (delivery.Courier != null && delivery.Status != DeliveryStatus.Completed)
            delivery.Courier.IsAvailable = true;

        _db.Deliveries.Remove(delivery);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
