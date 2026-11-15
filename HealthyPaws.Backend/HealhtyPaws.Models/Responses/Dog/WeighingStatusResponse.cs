namespace HealhtyPaws.Models.Responses.Dog;

/// <param name="NextMeasureAt">Kad agent očekuje sljedeće vaganje; null ako pas još nema plan (nikad vagan).</param>
/// <param name="IsDue">Vaganje je dospjelo (rok prošao ili još nema mjerenja).</param>
/// <param name="DaysOverdue">Puni dani kašnjenja; 0 ako nije dospjelo ili tek dospijeva.</param>
public record WeighingStatusResponse(int DogId, DateTime? NextMeasureAt, bool IsDue, int DaysOverdue);
