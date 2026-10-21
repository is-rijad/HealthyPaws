using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Requests.User;

public record UserUpdateRequest(
    [Required]
    [MinLength(1), MaxLength(32)]
    string FirstName,
    [Required]
    [MinLength(1), MaxLength(64)]
    string LastName,
    [Required][EmailAddress] string Email,
    [Required] bool ChangePassword,
    string? OldPassword,
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")]
    string? Password,
    string? PasswordAgain) {
    [JsonIgnore]
    public string? PasswordHash { get; set; }

    [JsonIgnore]
    public string? PasswordSalt { get; set; }
}