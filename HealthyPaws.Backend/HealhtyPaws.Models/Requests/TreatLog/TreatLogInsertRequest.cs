using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.TreatLog;

public class TreatLogInsertRequest
{
    [Required]
    public int DogId { get; set; }

    [Required]
    public int TreatId { get; set; }

    [Required]
    [Range(1, 1000)]
    public int Quantity { get; set; }

    public DateTime? GivenAt { get; set; }
}
