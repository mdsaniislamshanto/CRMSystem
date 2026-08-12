using CRMSystem.Constants;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }


        // =====================================================
        // GET: Notification/Index
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var notifications =
                await _notificationService
                    .GetUserNotificationsAsync(userId.Value);

            return View(notifications);
        }


        // =====================================================
        // GET: Notification/UnreadCount
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Json(new
                {
                    success = false,
                    count = 0
                });
            }

            var count =
                await _notificationService
                    .GetUnreadCountAsync(userId.Value);

            return Json(new
            {
                success = true,
                count
            });
        }


        // =====================================================
        // GET: Notification/Recent
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Json(new
                {
                    success = false,
                    notifications = Array.Empty<object>()
                });
            }

            var notifications =
                await _notificationService
                    .GetUserNotificationsAsync(userId.Value);

            var recentNotifications =
                notifications
                    .Take(5)
                    .Select(n => new
                    {
                        n.NotificationId,
                        n.NotificationType,
                        n.Title,
                        n.Message,
                        n.LeadId,
                        n.AssignmentId,
                        n.IsRead,
                        n.CreatedAt
                    })
                    .ToList();

            return Json(new
            {
                success = true,
                notifications = recentNotifications
            });
        }


        // =====================================================
        // POST: Notification/MarkAsRead
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(
            long notificationId)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            await _notificationService.MarkAsReadAsync(
                notificationId,
                userId.Value);

            return Ok(new
            {
                success = true
            });
        }


        // =====================================================
        // POST: Notification/MarkAllAsRead
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            await _notificationService
                .MarkAllAsReadAsync(userId.Value);

            return Ok(new
            {
                success = true
            });
        }


        // =====================================================
        // Helper
        // =====================================================

        private long? GetCurrentUserId()
        {
            var userId =
                HttpContext.Session.GetString(
                    SessionKeys.UserId);

            if (long.TryParse(
                userId,
                out long id))
            {
                return id;
            }

            return null;
        }
    }
}