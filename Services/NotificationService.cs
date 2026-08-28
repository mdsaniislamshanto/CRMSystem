using CRMSystem.Data;
using CRMSystem.Enums;
using CRMSystem.Models.Entities;
using CRMSystem.Models.ViewModels;
using CRMSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CRMSystem.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(
            ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateNotificationAsync(
    long userId,
    NotificationType notificationType,
    string title,
    string message,
    long? leadId = null,
    long? assignmentId = null)
        {
            // =====================================================
            // Get System Notification Settings
            // =====================================================

            var settings = await _context.SystemSettings
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                return;
            }


            // =====================================================
            // Global Notification Check
            // =====================================================

            if (!settings.GlobalNotificationsEnabled)
            {
                return;
            }


            // =====================================================
            // Notification Type Check
            // =====================================================

            bool notificationTypeEnabled = notificationType switch
            {
                NotificationType.LeadAssigned =>
                    settings.LeadAssignmentNotificationEnabled,

                NotificationType.AcceptanceSLAMissed =>
                    settings.OverdueNotificationEnabled,

                NotificationType.FirstFeedbackSLAMissed =>
                    settings.FeedbackNotificationEnabled,

                NotificationType.NextFeedbackOverdue =>
                    settings.FollowUpNotificationEnabled,

                _ => true
            };


            if (!notificationTypeEnabled)
            {
                return;
            }


            // =====================================================
            // User Notification Preference
            // =====================================================

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.IsActive);

            if (user == null)
            {
                return;
            }


            if (!user.NotificationsEnabled)
            {
                return;
            }


            // =====================================================
            // Create Notification
            // =====================================================

            var notification = new Notification
            {
                UserId = userId,

                NotificationType = notificationType,

                Title = title.Trim(),

                Message = message.Trim(),

                LeadId = leadId,

                AssignmentId = assignmentId,

                IsRead = false,

                CreatedAt = DateTime.UtcNow,

                IsDeleted = false
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }


        public async Task<List<NotificationViewModel>>
            GetUserNotificationsAsync(long userId)
        {
            return await _context.Notifications
                .Where(n =>
                    n.UserId == userId &&
                    !n.IsDeleted)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,

                    NotificationType = n.NotificationType,

                    Title = n.Title,

                    Message = n.Message,

                    LeadId = n.LeadId,

                    AssignmentId = n.AssignmentId,

                    IsRead = n.IsRead,

                    CreatedAt = n.CreatedAt,

                    ReadAt = n.ReadAt
                })
                .ToListAsync();
        }


        public async Task<int> GetUnreadCountAsync(
            long userId)
        {
            return await _context.Notifications
                .CountAsync(n =>
                    n.UserId == userId &&
                    !n.IsRead &&
                    !n.IsDeleted);
        }


        public async Task MarkAsReadAsync(
            long notificationId,
            long userId)
        {
            var notification =
                await _context.Notifications
                    .FirstOrDefaultAsync(n =>
                        n.NotificationId == notificationId &&
                        n.UserId == userId &&
                        !n.IsDeleted);

            if (notification == null)
            {
                return;
            }

            notification.IsRead = true;

            notification.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }


        public async Task MarkAllAsReadAsync(
            long userId)
        {
            var notifications =
                await _context.Notifications
                    .Where(n =>
                        n.UserId == userId &&
                        !n.IsRead &&
                        !n.IsDeleted)
                    .ToListAsync();

            if (!notifications.Any())
            {
                return;
            }

            var readAt = DateTime.UtcNow;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;

                notification.ReadAt = readAt;
            }

            await _context.SaveChangesAsync();
        }
    }
}