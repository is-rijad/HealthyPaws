using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.Device;

public class DeviceUpdateRequest
{
    [Required]
    public int DogId { get; set; }
}
