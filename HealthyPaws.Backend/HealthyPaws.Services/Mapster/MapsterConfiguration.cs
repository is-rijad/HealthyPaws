using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.Responses.Device;
using HealthyPaws.Services.AgentInsights;
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
                    : src.Dog.DogWeights.Where(w => !w.IsRejected).OrderByDescending(w => w.CreatedAt).FirstOrDefault());

        config.NewConfig<Database.Entity.CalorieAgent.Alert, AgentAlertResponse>()
            .MapToConstructor(true)
            .Map(dest => dest.Category, src => AlertAdvice.CategoryOf(src.Reason))
            .Map(dest => dest.RecommendedAction, src => AlertAdvice.RecommendationOf(src.Reason));
    }
}