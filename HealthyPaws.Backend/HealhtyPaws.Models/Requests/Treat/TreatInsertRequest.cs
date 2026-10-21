using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.Treat;

public class TreatInsertRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(64)]
    public string Name { get; set; } = null!;

    [Required]
    [Range(0.01, double.MaxValue)]
    public double CaloriesPerPiece { get; set; }
}
