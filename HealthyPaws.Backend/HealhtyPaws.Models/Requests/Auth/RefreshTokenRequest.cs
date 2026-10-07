using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.Auth;

public record RefreshTokenRequest(
    [Required]
    string AccessToken,
    [Required]
    string RefreshToken);