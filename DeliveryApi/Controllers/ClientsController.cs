using DeliveryApi.Data;
using DeliveryApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryApi.Controllers;

[Authorize(Roles = Roles.AllStaff)]
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

    // Создание/изменение/удаление клиентов отключено: клиенты регистрируются
    // самостоятельно через клиентский портал.
}
