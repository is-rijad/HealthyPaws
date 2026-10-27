using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HealhtyPaws.Models.Authorization;
using HealthyPaws.Database;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Authorization;

public class TokenManager(
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    HealthyPawsDbContext dbContext,
    IMapper mapper)
{
    private string JwtKey =>
        configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");

    private string JwtIssuer => configuration["Jwt:Issuer"] ??
                                throw new InvalidOperationException("JWT Issuer is not configured.");

    private string JwtExpiration => configuration["Jwt:ExpirationInMinutes"] ??
                                    throw new InvalidOperationException("JWT Expiration is not configured.");

    private string RefreshJwtExpiration => configuration["Jwt:RefreshExpirationInMinutes"] ??
                                           throw new InvalidOperationException(
                                               "JWT RefreshExpiration is not configured.");

    public async Task<string> GenerateToken(Database.Entity.User user) {
        var tokenDto = await GetTokenDto(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, tokenDto.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            JwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(JwtExpiration)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task DropRefreshTokenIfExists(int userId) {
        await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ExecuteDeleteAsync();
    }


    public UserInfoTokenDto GetUserInfoFromToken(string token) {
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        if (jwtToken == null)
            throw new InvalidOperationException("Invalid token!");
        var id = int.Parse(jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);

        return new UserInfoTokenDto(id);
    }

    public async Task<string> GenerateRefreshToken(Database.Entity.User user) {
        var tokenDto = await GetTokenDto(user);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, tokenDto.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            JwtIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(RefreshJwtExpiration)),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int GetUserId() {
        return Convert.ToInt32(httpContextAccessor.HttpContext!.User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
    }


    private async Task<UserInfoTokenDto> GetTokenDto(Database.Entity.User user) {
        return mapper.Map<UserInfoTokenDto>(user);;
    }
}