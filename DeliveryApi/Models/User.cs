using System.ComponentModel.DataAnnotations;

namespace DeliveryApi.Models;

public enum UserRole
{
    Admin      = 1,
    Client     = 2,
    Courier    = 3,
    Operator   = 4,
    Accountant = 5,
}

// Удобные комбинации ролей для [Authorize(Roles = ...)]
public static class Roles
{
    public const string Admin      = nameof(UserRole.Admin);
    public const string Operator   = nameof(UserRole.Operator);
    public const string Courier    = nameof(UserRole.Courier);
    public const string Accountant = nameof(UserRole.Accountant);

    public const string AllStaff       = "Admin,Operator,Courier,Accountant";
    public const string AdminOrOperator = "Admin,Operator";
    public const string Accounting     = "Admin,Accountant";
    public const string Dispatch       = "Admin,Operator,Courier";
}

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

    public UserRole Role { get; set; } = UserRole.Operator;

    public DateTime CreatedAt { get; set; }
}
