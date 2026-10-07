using HealthyPaws.Database.Entity;
using System.Security.Cryptography;
using HealhtyPaws.Models.Requests.Auth;
using HealhtyPaws.Models.Responses.Auth;
using HealthyPaws.Database;
using HealthyPaws.Services.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Authorization;

public class AuthorizationService(
    HealthyPawsDbContext context,
    TokenManager tokenManager)
{
    private const int HashByteSize = 32;
    private const int SaltByteSize = 16;
    private const int Iterations = 100000;

    private readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA256;

    public async Task<AccessTokenResponse> Login(UserLoginRequest request) {
        Database.Entity.User? user = await context.Users.FirstOrDefaultAsync(u =>
            u.Email == request.Email);
                if (user == null || !VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new BadRequestException("Uneseni podaci nisu ispravni!");
        
        var token = await tokenManager.GenerateToken(user);
        var refreshToken = await tokenManager.GenerateRefreshToken(user);

        await ChangeOrAddRefreshToken(refreshToken, user.Id);

        return new AccessTokenResponse(token, refreshToken);

    }


    private async Task ChangeOrAddRefreshToken(string refreshToken, int userId) {
        var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.UserId == userId);
        HashRefreshToken(refreshToken, out var hash, out var salt);

        if (refreshTokenEntity == null) {
            await context.RefreshTokens.AddAsync(new RefreshToken
            { TokenHash = hash, UserId = userId, TokenSalt = salt });
        }
        else {
            refreshTokenEntity.TokenHash = hash;
            refreshTokenEntity.TokenSalt = salt;
        }

        await context.SaveChangesAsync();
    }

    public async Task<AccessTokenResponse> Refresh(RefreshTokenRequest request) {
        var userId = tokenManager.GetUserInfoFromToken(request.AccessToken).Id;
        Database.Entity.User? user = await context.Users.FirstOrDefaultAsync(it => it.Id == userId);
        if (user == null)
        {
            throw new BadRequestException("Korisnik nije pronađen!");
        }
        var refreshTokenEntity = await context.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.UserId == userId);

        if (refreshTokenEntity == null) throw new BadRequestException("Korisnik nije ulogovan!");

        if (!VerifyRefreshToken(request.RefreshToken, refreshTokenEntity.TokenHash, refreshTokenEntity.TokenSalt))
            throw new BadRequestException("Neispravan refresh token!");

        var token = await tokenManager.GenerateToken(user);
        var refreshToken = await tokenManager.GenerateRefreshToken(user);

        await ChangeOrAddRefreshToken(refreshToken, userId);

        return new AccessTokenResponse(token, refreshToken);

    }

    public void HashRefreshToken(string refreshToken, out string hash, out string salt) {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[SaltByteSize];
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(refreshToken, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        hash = Convert.ToBase64String(hashBytes);
    }

    public bool VerifyRefreshToken(string refreshToken, string storedHash, string storedSalt) {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var pbkdf2 = new Rfc2898DeriveBytes(refreshToken, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        var hash = Convert.ToBase64String(hashBytes);
        return hash == storedHash;
    }

    public async Task<AccessTokenResponse> GetNewToken() {
        var userId = GetUserId();

        var user = await context.Users.FirstOrDefaultAsync(u =>
            u.Id == userId);
        if (user == null) throw new UnauthorizedException("Korisnik nije pronađen!");

        var token = await tokenManager.GenerateToken(user);
        var refreshToken = await tokenManager.GenerateRefreshToken(user);

        await ChangeOrAddRefreshToken(refreshToken, userId);

        return new AccessTokenResponse(token, refreshToken);
    }

    public void CreatePassword(string password, string passwordAgain, out string hash, out string salt) {
        if (password != passwordAgain)
            throw new BadRequestException("Lozinke se ne podudaraju!");
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[SaltByteSize];
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        hash = Convert.ToBase64String(hashBytes);
    }

    public string CreateResetPasswordCode(out string code, out string salt, out DateTime expiration) {
        code = RandomNumberGenerator.GetInt32(10_000_000, 100_000_000).ToString();

        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[SaltByteSize];
        rng.GetBytes(saltBytes);
        salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(code, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        var hashString = Convert.ToBase64String(hashBytes);
        expiration = DateTime.UtcNow.AddMinutes(15);

        return hashString;
    }

    public void VerifyResetPasswordCode(string code, string realHash, string salt, DateTime expiration) {
        if (expiration < DateTime.UtcNow)
            throw new BadRequestException("Reset kod je istekao! Molimo zatražite novi kod.");

        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(code, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        var hashString = Convert.ToBase64String(hashBytes);
        if (hashString != realHash)
            throw new BadRequestException("Kod nije ispravan!");
    }


    public bool VerifyPassword(string password, string storedHash, string storedSalt) {
        var saltBytes = Convert.FromBase64String(storedSalt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, _hashAlgorithm);
        var hashBytes = pbkdf2.GetBytes(HashByteSize);
        var hash = Convert.ToBase64String(hashBytes);
        return hash == storedHash;
    }

    public int GetUserId() {
        return tokenManager.GetUserId();
    }

    public async Task Logout() {
        var userId = GetUserId();
        await tokenManager.DropRefreshTokenIfExists(userId);
    }
}