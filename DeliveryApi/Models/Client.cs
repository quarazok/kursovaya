using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public class Client
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Имя обязательно")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Фамилия обязательна")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефон обязателен")]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    [EmailAddress(ErrorMessage = "Некорректный формат email")]
    public string? Email { get; set; }

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
