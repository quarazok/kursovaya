using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public enum PaymentMethod
{
    Cash,
    Card,
    Transfer
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Cancelled
}

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required(ErrorMessage = "Сумма обязательна")]
    [Range(0.01, 10000000, ErrorMessage = "Сумма должна быть больше 0")]
    public decimal Amount { get; set; }

    public DateTime? PaidAt { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}
