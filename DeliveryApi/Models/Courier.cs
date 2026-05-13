using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public class Courier
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

    public bool IsAvailable { get; set; } = true;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
