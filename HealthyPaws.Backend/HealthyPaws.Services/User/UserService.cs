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
    AuthorizationService authorizationService) : BaseCrudService<Database.Entity.User, UserInsertRequest, UserInsertRequest, UserResponse, BaseSearchObject>(dbContext, mapper, authorizationService), IUserService
{
    protected override async Task BeforeInsert(UserInsertRequest request)
    {
        await base.BeforeInsert(request);
        
        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email)) {
            throw new BadRequestException("Korisnik sa unesenim emailom već postoji!");
        }

        CreateAndAssignPassword(request);
    }

    private void CreateAndAssignPassword(UserInsertRequest request)
    {
        authorizationService.CreatePassword(request.Password, request.PasswordAgain, out var hash, out var salt);
        request.PasswordHash = hash;
        request.PasswordSalt = salt;
    }
}