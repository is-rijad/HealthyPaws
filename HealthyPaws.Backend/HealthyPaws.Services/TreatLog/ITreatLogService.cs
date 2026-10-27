using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.Responses.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.TreatLog;

public interface ITreatLogService : IBaseCrudService<TreatLogInsertRequest, TreatLogInsertRequest, TreatLogResponse, TreatLogSearchObject>
{
    public Task<double> GetTreatCaloriesAsync(int dogId, DateTime? from, DateTime? to);
}
