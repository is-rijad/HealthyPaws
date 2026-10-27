using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HealhtyPaws.Models.Requests.User;

public record UserInsertRequest(
    [Required] [EmailAddress] string Email,
    [Required]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")]
    string Password,
    [Required] string PasswordAgain)
{
    [JsonIgnore]
    public string? PasswordHash { get; set; }

    [JsonIgnore]
    public string? PasswordSalt { get; set; }
}