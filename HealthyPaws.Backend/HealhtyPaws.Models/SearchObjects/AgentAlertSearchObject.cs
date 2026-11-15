using HealhtyPaws.Models.BaseModels;

namespace HealhtyPaws.Models.SearchObjects;

public class AgentAlertSearchObject : BaseSearchObject
{
    public int? DogId { get; set; }
    /// <summary>true = samo otvoreni, false = samo riješeni, null = svi.</summary>
    public bool? IsOpen { get; set; }
}
