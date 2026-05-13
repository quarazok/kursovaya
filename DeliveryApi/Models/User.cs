using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public enum UserRole { Employee = 0, Admin = 1, Client = 2 }

public class User
{
    public int Id { get; set; }

    [Required][MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required][MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required][MaxLength(200)]
    public string Email { get; set; } = "";

    [Required][MaxLength(20)]
    public string Phone { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Employee;

    public DateTime CreatedAt { get; set; }
}
