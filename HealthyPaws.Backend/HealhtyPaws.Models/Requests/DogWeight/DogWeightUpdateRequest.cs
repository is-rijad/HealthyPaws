using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.DogWeight;

public class DogWeightUpdateRequest
{
    public int? Bcs { get; set; }
    [Range(0.01, double.MaxValue)]
    public double? Weight { get; set; }
    [Range(0.01, double.MaxValue)]
    public double? Calorie { get; set; }
}
