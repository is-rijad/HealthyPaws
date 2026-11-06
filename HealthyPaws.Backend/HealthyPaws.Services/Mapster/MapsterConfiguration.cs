using HealhtyPaws.Models.Responses.Device;
using Mapster;

namespace HealthyPaws.Services.Mapster;

public class MapsterConfiguration : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Database.Entity.Device, DeviceResponse>()
            .Map(dest => dest.LastWeightRecord,
                src => src.Dog == null || src.Dog.DogWeights == null
                    ? null
                    : src.Dog.DogWeights.OrderByDescending(w => w.CreatedAt).FirstOrDefault());
        
    }
}