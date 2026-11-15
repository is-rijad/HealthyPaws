using HealthyPaws.Database.Entity;
using HealhtyPaws.Models.Responses.Agent;

namespace HealhtyPaws.Models.Responses.DogWeight;

public class DogWeightResponse
{
    public int Id { get; set; }
    public int Bcs { get; set; }
    public double Weight { get; set; }
    public double Calorie { get; set; }
    public CalorieSource CalorieSource { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRejected { get; set; }
    /// <summary>Odluka agenta; popunjeno samo u odgovoru na unos mjerenja.</summary>
    public AgentDecisionResponse? Agent { get; set; }
}
