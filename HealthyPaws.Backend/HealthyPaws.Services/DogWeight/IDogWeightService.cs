using HealhtyPaws.Models.Responses.DogWeight;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.DogWeight;

public interface IDogWeightService : IBaseService<DogWeightResponse, DogWeightSearchObject>
{
}
