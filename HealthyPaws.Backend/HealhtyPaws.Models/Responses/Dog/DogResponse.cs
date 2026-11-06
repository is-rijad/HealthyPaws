using HealhtyPaws.Models.Responses.Breed;

namespace HealhtyPaws.Models.Responses.Dog;

public record DogResponse(
    int Id,
    string Name,
    int BreedId,
    DateTime DateOfBirth,
    short Gender,
    float ActivityStatus,
    bool IsNeutered,
    BreedResponse? Breed);