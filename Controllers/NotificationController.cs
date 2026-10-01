using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PadiPay.Models;
using PadiPay.Services;

namespace PadiPay.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // GET /Notification/Index (Full notification center)
    public async Task<IActionResult> Index(string filter = "all")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        ViewBag.Filter = filter.ToLowerInvariant();
        var allNotifications = await _notificationService.GetRecentNotificationsAsync(userId, 50);

        if (filter.ToLowerInvariant() == "unread")
        {
            allNotifications = allNotifications.Where(n => !n.IsRead).ToList();
        }

        ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId);

        return View(allNotifications);
    }

    // GET /Notification/GetUnreadCount (JSON for navbar polling / dynamic update)
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { count = 0 });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }

    // GET /Notification/GetRecent (JSON for navbar dropdown)
    [HttpGet]
    public async Task<IActionResult> GetRecent()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { notifications = new List<object>() });
        }

        var list = await _notificationService.GetRecentNotificationsAsync(userId, 8);
        var dtos = list.Select(n => new
        {
            id = n.Id,
            title = n.Title,
            message = n.Message,
            targetUrl = n.TargetUrl ?? "#",
            type = n.Type.ToString(),
            isRead = n.IsRead,
            createdAtFormatted = GetTimeAgo(n.CreatedAt)
        });

        return Json(new { notifications = dtos });
    }

    // POST /Notification/MarkAsRead
    [HttpPost]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var success = await _notificationService.MarkAsReadAsync(id, userId);
        return Json(new { success });
    }

    // POST /Notification/MarkAllAsRead
    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(userId);
        return Json(new { success = true });
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var span = DateTime.UtcNow - dateTime;
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
        return dateTime.ToString("MMM dd, yyyy");
    }
}
