using System.IdentityModel.Tokens.Jwt;
using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Auth;
using HealhtyPaws.Models.Requests.User;
using HealthyPaws.Database.Entity;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.User;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Tests.Services;

public class AuthTests : IDisposable
{
    const string Password = "Lozinka1!";
    readonly TestDb _db = new();
    public void Dispose() => _db.Dispose();

    AuthorizationService Auth(int? userId = null) => TestServices.Auth(_db.Create(), userId);
    UserService Users(int? userId = null)
    {
        var ctx = _db.Create();
        return new UserService(ctx, TestServices.Mapper(), TestServices.Auth(ctx, userId));
    }

    async Task<int> Register(string email = "ana@test.ba", string password = Password)
    {
        await Users().InsertAsync(new UserInsertRequest(email, password, password));
        return (await _db.Create().Users.SingleAsync(u => u.Email == email)).Id;
    }

    // ---------------------------------------------------------------- lozinke

    [Fact]
    public void Password_hash_roundtrip_and_salt_is_random()
    {
        var auth = Auth();
        auth.CreatePassword("Abc123!x", "Abc123!x", out var hash1, out var salt1);
        auth.CreatePassword("Abc123!x", "Abc123!x", out var hash2, out var salt2);
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(hash1, hash2);
        Assert.True(auth.VerifyPassword("Abc123!x", hash1, salt1));
        Assert.False(auth.VerifyPassword("abc123!x", hash1, salt1));
        Assert.False(auth.VerifyPassword("Abc123!x", hash2, salt1));
    }

    [Fact]
    public void Hash_and_salt_fit_database_column_sizes()
    {
        Auth().CreatePassword("Abc123!x", "Abc123!x", out var hash, out var salt);
        Assert.Equal(44, hash.Length);  // PasswordHash: fixed length 44
        Assert.Equal(24, salt.Length);  // PasswordSalt: fixed length 24
    }

    [Fact]
    public void Mismatched_passwords_are_rejected()
    {
        var ex = Assert.Throws<BadRequestException>(() => Auth().CreatePassword("a", "b", out _, out _));
        Assert.Contains("ne podudaraju", ex.Message);
    }

    [Fact]
    public void Reset_code_verification()
    {
        var auth = Auth();
        var hash = auth.CreateResetPasswordCode(out var code, out var salt, out var expiration);
        Assert.Matches(@"^\d{8}$", code);
        Assert.InRange(expiration, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));

        auth.VerifyResetPasswordCode(code, hash, salt, expiration);   // ne baca
        Assert.Contains("nije ispravan", Assert.Throws<BadRequestException>(() => auth.VerifyResetPasswordCode("00000000", hash, salt, expiration)).Message);
        Assert.Contains("istekao", Assert.Throws<BadRequestException>(() => auth.VerifyResetPasswordCode(code, hash, salt, DateTime.UtcNow.AddSeconds(-1))).Message);
    }

    // ---------------------------------------------------------------- registracija

    [Fact]
    public async Task Register_stores_hashed_password_never_plain_text()
    {
        var id = await Register();
        var user = await _db.Create().Users.SingleAsync(u => u.Id == id);
        Assert.DoesNotContain(Password, user.PasswordHash);
        Assert.True(Auth().VerifyPassword(Password, user.PasswordHash, user.PasswordSalt));
    }

    [Fact]
    public async Task Register_duplicate_email_is_rejected()
    {
        await Register("dup@test.ba");
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Register("dup@test.ba"));
        Assert.Contains("već postoji", ex.Message);
        Assert.Equal(1, await _db.Create().Users.CountAsync());
    }

    [Fact]
    public async Task Register_with_mismatched_passwords_creates_nothing()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => Users().InsertAsync(new UserInsertRequest("x@test.ba", Password, "Druga1!xx")));
        Assert.Equal(0, await _db.Create().Users.CountAsync());
    }

    [Theory]
    [InlineData("Abcdef1!", true)]
    [InlineData("abcdef1!", false)]   // nema velikog slova
    [InlineData("ABCDEF1!", false)]   // nema malog slova
    [InlineData("Abcdefg!", false)]   // nema cifre
    [InlineData("Abcdefg1", false)]   // nema specijalnog znaka
    [InlineData("Abc1!", false)]      // prekratka
    public void Password_policy_regex(string password, bool valid)
    {
        var attribute = typeof(UserInsertRequest).GetConstructors()[0].GetParameters()[1]
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RegularExpressionAttribute), false)
            .Cast<System.ComponentModel.DataAnnotations.RegularExpressionAttribute>().Single();
        Assert.Equal(valid, attribute.IsValid(password));
    }

    // ---------------------------------------------------------------- login / refresh / logout

    [Fact]
    public async Task Login_returns_tokens_and_stores_hashed_refresh_token()
    {
        var id = await Register();
        var tokens = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));

        var access = new JwtSecurityTokenHandler().ReadJwtToken(tokens.Token);
        Assert.Equal(id.ToString(), access.Subject);
        Assert.Equal("healthy-paws-tests", access.Issuer);
        Assert.InRange(access.ValidTo, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));

        var stored = await _db.Create().RefreshTokens.SingleAsync();
        Assert.NotEqual(tokens.RefreshToken, stored.TokenHash);
        Assert.True(Auth().VerifyRefreshToken(tokens.RefreshToken, stored.TokenHash, stored.TokenSalt));
    }

    [Theory]
    [InlineData("ana@test.ba", "Pogresna1!")]
    [InlineData("nepostojeci@test.ba", Password)]
    public async Task Login_failure_gives_same_generic_message(string email, string password)
    {
        await Register();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Auth().Login(new UserLoginRequest(email, password)));
        Assert.Equal("Uneseni podaci nisu ispravni!", ex.Message);   // ne otkriva da li email postoji
    }

    [Fact]
    public async Task Second_login_replaces_the_refresh_token_instead_of_adding_one()
    {
        await Register();
        var first = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));
        var second = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));
        Assert.Equal(1, await _db.Create().RefreshTokens.CountAsync());
        var stored = await _db.Create().RefreshTokens.SingleAsync();
        Assert.False(Auth().VerifyRefreshToken(first.RefreshToken, stored.TokenHash, stored.TokenSalt));
        Assert.True(Auth().VerifyRefreshToken(second.RefreshToken, stored.TokenHash, stored.TokenSalt));
    }

    [Fact]
    public async Task Refresh_rotates_tokens_and_old_refresh_token_stops_working()
    {
        await Register();
        var login = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));
        var refreshed = await Auth().Refresh(new RefreshTokenRequest(login.Token, login.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);

        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Auth().Refresh(new RefreshTokenRequest(login.Token, login.RefreshToken)));
        Assert.Contains("Neispravan refresh token", ex.Message);
        await Auth().Refresh(new RefreshTokenRequest(refreshed.Token, refreshed.RefreshToken));
    }

    [Fact]
    public async Task Refresh_works_with_expired_access_token_since_only_subject_is_read()
    {
        await Register();
        var login = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));
        var expired = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            "healthy-paws-tests", claims: new JwtSecurityTokenHandler().ReadJwtToken(login.Token).Claims,
            notBefore: DateTime.UtcNow.AddHours(-3), expires: DateTime.UtcNow.AddHours(-2)));
        await Auth().Refresh(new RefreshTokenRequest(expired, login.RefreshToken));
    }

    [Fact]
    public async Task Refresh_when_logged_out_fails()
    {
        var id = await Register();
        var login = await Auth().Login(new UserLoginRequest("ana@test.ba", Password));
        await Auth(id).Logout();
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => Auth().Refresh(new RefreshTokenRequest(login.Token, login.RefreshToken)));
        Assert.Contains("nije ulogovan", ex.Message);
        Assert.Equal(0, await _db.Create().RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task GetNewToken_for_unknown_user_is_unauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Auth(userId: 4242).GetNewToken());
    }

    // ---------------------------------------------------------------- TokenManager

    [Fact]
    public async Task Token_manager_requires_configuration()
    {
        var ctx = _db.Create();
        var manager = new TokenManager(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            new Microsoft.AspNetCore.Http.HttpContextAccessor(), ctx, TestServices.Mapper());
        var user = new User { Id = 1, Email = "a@b.c" };
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GenerateToken(user));
    }

    [Fact]
    public async Task Tokens_for_same_user_are_unique_and_carry_user_id()
    {
        var ctx = _db.Create();
        var manager = new TokenManager(TestServices.Jwt(), new Microsoft.AspNetCore.Http.HttpContextAccessor(), ctx, TestServices.Mapper());
        var user = new User { Id = 77, Email = "a@b.c" };
        var a = await manager.GenerateToken(user);
        var b = await manager.GenerateToken(user);
        Assert.NotEqual(a, b);                                  // jti
        Assert.Equal(77, manager.GetUserInfoFromToken(a).Id);
        Assert.Equal(77, manager.GetUserInfoFromToken(await manager.GenerateRefreshToken(user)).Id);
    }

    [Fact]
    public async Task Refresh_token_outlives_access_token()
    {
        var ctx = _db.Create();
        var manager = new TokenManager(TestServices.Jwt(), new Microsoft.AspNetCore.Http.HttpContextAccessor(), ctx, TestServices.Mapper());
        var user = new User { Id = 1, Email = "a@b.c" };
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.ReadJwtToken(await manager.GenerateRefreshToken(user)).ValidTo
                    > handler.ReadJwtToken(await manager.GenerateToken(user)).ValidTo);
    }

    // ---------------------------------------------------------------- promjena profila

    [Fact]
    public async Task Update_cannot_take_another_users_email()
    {
        await Register("a@test.ba");
        var bId = await Register("b@test.ba");
        await Assert.ThrowsAsync<BadRequestException>(() => Users(bId).UpdateAsync(bId, new UserUpdateRequest("a@test.ba", false, null, null, null)));
    }

    [Fact]
    public async Task Password_change_requires_correct_old_password()
    {
        var id = await Register();
        var wrong = new UserUpdateRequest("ana@test.ba", true, "Kriva1!xx", "Nova1!xxx", "Nova1!xxx");
        Assert.Contains("Stara lozinka", (await Assert.ThrowsAsync<BadRequestException>(() => Users(id).UpdateAsync(id, wrong))).Message);

        var missing = new UserUpdateRequest("ana@test.ba", true, Password, null, null);
        Assert.Contains("polja", (await Assert.ThrowsAsync<BadRequestException>(() => Users(id).UpdateAsync(id, missing))).Message);

        var ok = new UserUpdateRequest("ana@test.ba", true, Password, "Nova1!xxx", "Nova1!xxx");
        await Users(id).UpdateAsync(id, ok);
        await Auth().Login(new UserLoginRequest("ana@test.ba", "Nova1!xxx"));
        await Assert.ThrowsAsync<BadRequestException>(() => Auth().Login(new UserLoginRequest("ana@test.ba", Password)));
    }
}
