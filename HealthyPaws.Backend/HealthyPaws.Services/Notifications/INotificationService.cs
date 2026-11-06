using CalorieAgent;
using HealthyPaws.Database.Entity.CalorieAgent;

namespace HealthyPaws.Services.Notifications;

public interface INotificationService
{
    public Task SendAlertAsync(Alert alert, CalorieAgentResponse response);
}