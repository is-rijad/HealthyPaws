namespace HealhtyPaws.Models.Dto.Bandit;

/// <param name="LocalBaseline">Lokalno (Kind=Unspecified) vrijeme baselinea, već s datumom (može biti sutradan za kasne obroke).</param>
public record BanditSlot(int DogId, int MealIndex, DateTime LocalBaseline);
