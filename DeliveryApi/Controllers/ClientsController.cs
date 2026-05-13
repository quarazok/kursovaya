using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/clients
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var clients = await _db.Clients.ToListAsync();
        return Ok(clients);
    }

    // GET api/clients/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var client = await _db.Clients.FindAsync(id);

        if (client == null)
            return NotFound();

        return Ok(client);
    }

    // POST api/clients
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Client client)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    // PUT api/clients/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Client updated)
    {
        var client = await _db.Clients.FindAsync(id);

        if (client == null)
            return NotFound();

        client.FirstName = updated.FirstName;
        client.LastName  = updated.LastName;
        client.Phone     = updated.Phone;
        client.Email     = updated.Email;

        await _db.SaveChangesAsync();
        return Ok(client);
    }

    // DELETE api/clients/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients.FindAsync(id);

        if (client == null)
            return NotFound();

        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
