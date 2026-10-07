using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Responses.Dog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.Dog;

public interface IDogService : IBaseCrudService<DogInsertRequest, DogInsertRequest, DogResponse, DogSearchObject>
{
    
}