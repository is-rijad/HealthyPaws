using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Requests.DogWeight;
using HealhtyPaws.Models.Responses.Device;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.Device;

public interface IDeviceService : IBaseCrudService<DeviceInsertRequest, DeviceUpdateRequest, DeviceResponse, DeviceSearchObject>
{
    Task<DeviceResponse> AddWeightAsync(int deviceId, DogWeightInsertRequest request);
}