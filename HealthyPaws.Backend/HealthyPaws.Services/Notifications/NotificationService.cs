using CalorieAgent;
using HealthyPaws.Database.Entity.CalorieAgent;

namespace HealthyPaws.Services.Notifications;

public class NotificationService : INotificationService
{
    public Task SendAlertAsync(Alert alert, CalorieAgentResponse response)
    {
        return Task.CompletedTask;
    }
}