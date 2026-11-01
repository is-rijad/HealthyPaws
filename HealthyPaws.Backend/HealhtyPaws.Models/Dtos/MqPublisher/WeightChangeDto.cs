namespace HealhtyPaws.Models.Dtos.MqPublisher;

public record WeightChangeDto(int DogId, double OldWeight, double Weight, int Bcs, decimal MinWeight, decimal MaxWeight, DateTime MeasuredAt);