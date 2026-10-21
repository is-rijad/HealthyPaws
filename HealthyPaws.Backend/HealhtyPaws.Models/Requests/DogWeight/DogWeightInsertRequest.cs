using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.DogWeight;

public class DogWeightInsertRequest
{
    [Required]
    public int Bcs { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public double Weight { get; set; }
    [Required]
    [Range(0.01, double.MaxValue)]
    public double Calorie { get; set; }

    public DateTime? CreatedAt { get; set; }
}