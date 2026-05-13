using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public enum DeliveryStatus
{
    Assigned,
    InProgress,
    Completed,
    Failed
}

public class Delivery
{
    public int Id { get; set; }

    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Required]
    public int CourierId { get; set; }
    public Courier? Courier { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Assigned;
}
