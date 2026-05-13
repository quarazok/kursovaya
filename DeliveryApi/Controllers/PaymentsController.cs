using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/payments
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var payments = await _db.Payments
            .Include(p => p.Order)
            .ToListAsync();
        return Ok(payments);
    }

    // GET api/payments/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments
            .Include(p => p.Order)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
            return NotFound();

        return Ok(payment);
    }

    // POST api/payments
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Payment payment)
    {
        var order = await _db.Orders.FindAsync(payment.OrderId);
        if (order == null)
            return BadRequest("Заказ не найден");

        payment.Status = PaymentStatus.Pending;
        payment.PaidAt = null;

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = payment.Id }, payment);
    }

    // PUT api/payments/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Payment updated)
    {
        var payment = await _db.Payments.FindAsync(id);

        if (payment == null)
            return NotFound();

        payment.Amount        = updated.Amount;
        payment.PaymentMethod = updated.PaymentMethod;

        await _db.SaveChangesAsync();
        return Ok(payment);
    }

    // PATCH api/payments/5/status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] PaymentStatus status)
    {
        var payment = await _db.Payments.FindAsync(id);

        if (payment == null)
            return NotFound();

        payment.Status = status;

        // При оплате фиксируем дату
        if (status == PaymentStatus.Paid)
            payment.PaidAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(payment);
    }

    // DELETE api/payments/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var payment = await _db.Payments.FindAsync(id);

        if (payment == null)
            return NotFound();

        _db.Payments.Remove(payment);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
