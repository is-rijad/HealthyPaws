using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.Auth;

public record UserLoginRequest(
    [Required]
    [EmailAddress]
    string Email,
    [Required]
    string Password);