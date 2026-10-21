using HealhtyPaws.Models.BaseModels;

namespace HealhtyPaws.Models.SearchObjects;

public class DogWeightSearchObject : BaseSearchObject
{
    public int? DogId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public double? MinWeight { get; set; }
    public double? MaxWeight { get; set; }
    public int? Bcs { get; set; }

    /// <summary>createdAt (default), weight or bcs.</summary>
    public string? OrderBy { get; set; }
    public bool Descending { get; set; } = true;
}
