using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealthyPaws.Database;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using MapsterMapper;

namespace HealthyPaws.Services.Device;

public class DeviceService(HealthyPawsDbContext dbContext, IMapper mapper, AuthorizationService authorizationService) : BaseCrudService<Database.Entity.Device, DeviceInsertRequest, DeviceInsertRequest, DeviceResponse, BaseSearchObject>(dbContext, mapper, authorizationService), IDeviceService
{
    
}