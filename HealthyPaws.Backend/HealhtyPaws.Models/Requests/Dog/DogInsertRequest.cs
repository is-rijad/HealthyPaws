using System.ComponentModel.DataAnnotations;

namespace HealhtyPaws.Models.Requests.Dog;

public record DogInsertRequest(
    [Required]
    [MinLength(1)]
    [MaxLength(16)]
    string Name, 
    [Required]
    int BreedId,
    [Required]
    DateTime DateOfBirth,
    [Required]
    [EnumDataType(typeof(DogGender))]
    short Gender,
    [Required]
    float ActivityStatus,
    [Required]
    bool IsNeutered
    );