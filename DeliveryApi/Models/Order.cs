using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public enum OrderStatus
{
    New,
    Assigned,
    InProgress,
    Delivered,
    Cancelled
}

public class Order
{
    public int Id { get; set; }

    [Required]
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    [Required(ErrorMessage = "Адрес отправления обязателен")]
    [MaxLength(300)]
    public string PickupAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "Адрес доставки обязателен")]
    [MaxLength(300)]
    public string DeliveryAddress { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public Delivery? Delivery { get; set; }
    public Payment? Payment { get; set; }
}
