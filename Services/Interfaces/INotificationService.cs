using CRMSystem.Enums;
using CRMSystem.Models.ViewModels;

namespace CRMSystem.Services.Interfaces
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            long userId,
            NotificationType notificationType,
            string title,
            string message,
            long? leadId = null,
            long? assignmentId = null);

        Task<List<NotificationViewModel>>
            GetUserNotificationsAsync(long userId);

        Task<int> GetUnreadCountAsync(long userId);

        Task MarkAsReadAsync(
            long notificationId,
            long userId);

        Task MarkAllAsReadAsync(long userId);
    }
}