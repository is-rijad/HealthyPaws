using System.ComponentModel.DataAnnotations;
using HealhtyPaws.Models.Requests.Dog;
using HealhtyPaws.Models.Requests.DogWeight;

namespace HealhtyPaws.Models.Requests.Device;

public class DeviceInsertRequest
{
    [Required]
    [RegularExpression("^[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}:[0-9A-F]{2}$")]
    public string MacAddress { get; set; } = null!;

    public DogInsertRequest? Dog { get; set; }
    public DogWeightInsertRequest? DogWeight { get; set; }
}