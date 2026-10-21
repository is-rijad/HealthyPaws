namespace HealhtyPaws.Models.Responses.TreatLog;

public class TreatLogResponse
{
    public int Id { get; set; }
    public int DogId { get; set; }
    public int? TreatId { get; set; }
    public string TreatName { get; set; } = null!;
    public int Quantity { get; set; }
    public double Calories { get; set; }
    public DateTime GivenAt { get; set; }
}
