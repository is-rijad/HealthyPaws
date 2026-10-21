using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.Responses.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.TreatLog;

public interface ITreatLogService : IBaseCrudService<TreatLogInsertRequest, TreatLogInsertRequest, TreatLogResponse, TreatLogSearchObject>
{
}
