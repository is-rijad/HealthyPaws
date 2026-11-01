using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.DogWeight;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.DogWeight;

public interface IDogWeightService : IBaseCrudService<DogWeightInsertRequest, DogWeightUpdateRequest, DogWeightResponse, DogWeightSearchObject>
{
    public Task<double> GetLatestDogWeightAsync(int dogId);
}
