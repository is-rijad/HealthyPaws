using HealhtyPaws.Models.Responses.Dog;
using HealhtyPaws.Models.Responses.DogWeight;

namespace HealhtyPaws.Models.Responses.Device;

public class DeviceResponse
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public string MacAddress { get; set; } = null!;
    public bool IsConnected { get; set; }
    public DogResponse? Dog { get; set; }
    public DogWeightResponse? LastWeightRecord { get; set; }

}