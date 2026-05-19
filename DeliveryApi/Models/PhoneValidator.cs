using System.Text.RegularExpressions;

namespace DeliveryApi.Models;

// Валидация телефона: только формат +7XXXXXXXXXX (12 символов).
// Достаточно для российского мобильного номера, и одинакова на бэке/фронте.
public static class PhoneValidator
{
    public const string Pattern = @"^\+7\d{10}$";
    public const string ErrorMessage = "Телефон должен быть в формате +7XXXXXXXXXX (12 символов)";

    private static readonly Regex Rx = new(Pattern, RegexOptions.Compiled);

    public static bool IsValid(string? phone) =>
        !string.IsNullOrWhiteSpace(phone) && Rx.IsMatch(phone);
}
