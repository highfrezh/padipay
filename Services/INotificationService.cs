using PadiPay.Models;

namespace PadiPay.Services;

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, string? targetUrl = null, NotificationType type = NotificationType.System);
    Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 10);
    Task<int> GetUnreadCountAsync(string userId);
    Task<bool> MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}
