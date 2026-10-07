using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.Device;

public interface IDeviceService : IBaseCrudService<DeviceInsertRequest, DeviceInsertRequest, DeviceResponse, BaseSearchObject>
{
    
}