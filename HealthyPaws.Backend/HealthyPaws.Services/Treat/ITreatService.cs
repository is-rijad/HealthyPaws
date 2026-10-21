using HealhtyPaws.Models.Requests.Treat;
using HealhtyPaws.Models.Responses.Treat;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.Treat;

public interface ITreatService : IBaseCrudService<TreatInsertRequest, TreatInsertRequest, TreatResponse, TreatSearchObject>
{
}
