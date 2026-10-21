using HealhtyPaws.Models.BaseModels;

namespace HealhtyPaws.Models.SearchObjects;

public class TreatLogSearchObject : BaseSearchObject
{
    public int? DogId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
