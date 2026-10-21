using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.User;
using HealhtyPaws.Models.Responses.User;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.User;

public interface IUserService : IBaseCrudService<UserInsertRequest, UserUpdateRequest, UserResponse, BaseSearchObject>
{
}