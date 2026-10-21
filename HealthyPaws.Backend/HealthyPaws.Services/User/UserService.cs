using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.User;
using HealhtyPaws.Models.Responses.User;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.User;

public class UserService(HealthyPawsDbContext dbContext,
    IMapper mapper,
    AuthorizationService authorizationService) : BaseCrudService<Database.Entity.User, UserInsertRequest, UserUpdateRequest, UserResponse, BaseSearchObject>(dbContext, mapper, authorizationService), IUserService
{
    protected override async Task BeforeInsert(UserInsertRequest request)
    {
        await base.BeforeInsert(request);
        
        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email)) {
            throw new BadRequestException("Korisnik sa unesenim emailom već postoji!");
        }

        authorizationService.CreatePassword(request.Password, request.PasswordAgain, out var hash, out var salt);
        request.PasswordHash = hash;
        request.PasswordSalt = salt;
    }

    protected override async Task BeforeUpdate(int id, UserUpdateRequest request, Database.Entity.User entity)
    {
        await base.BeforeUpdate(id, request, entity);
        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id)) {
            throw new BadRequestException("Korisnik sa unesenim emailom već postoji!");
        }
        if (request.ChangePassword)
        {
            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.PasswordAgain)) {
                throw new BadRequestException("Sva polja za promjenu lozinke moraju biti popunjena!");
            }
            if (!authorizationService.VerifyPassword(request.OldPassword, entity.PasswordHash, entity.PasswordSalt)) {
                throw new BadRequestException("Stara lozinka nije ispravna!");
            }
            authorizationService.CreatePassword(request.Password, request.PasswordAgain, out var hash, out var salt);
            request.PasswordHash = hash;
            request.PasswordSalt = salt;
        }
    }
}