using HealhtyPaws.Models.Responses.Dog;

namespace HealhtyPaws.Models.Responses.Device;

public class DeviceResponse
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public decimal CurrentWeight { get; set; }
    public bool IsConnected { get; set; }
    public DogResponse? Dog { get; set; }

}