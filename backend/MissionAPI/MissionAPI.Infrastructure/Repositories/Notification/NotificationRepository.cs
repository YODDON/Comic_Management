using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MissionAPI.Data;
using MissionAPI.Entities;
using MissionAPI.Interfaces;

namespace MissionAPI.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly MissionDbContext _context;

        public NotificationRepository(MissionDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize)
        {
            var query = _context.Notifications.AsQueryable();

            // Filter for this user OR broadcast (Guid.Empty)
            query = query.Where(n => n.IsActive && (n.UserId == userId || n.UserId == 0));

            if (isRead.HasValue)
            {
                if (isRead.Value)
                {
                    query = query.Where(n => n.IsRead);
                }
                else
                {
                    query = query.Where(n => !n.IsRead);
                }
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            var totalCount = await query.CountAsync();
            var notifications = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (notifications, totalCount);
        }

        public async Task<IEnumerable<Notification>> GetAllNotificationsForAdminAsync() =>
            await _context.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<Notification?> GetNotificationByIdAsync(Guid id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<Notification> CreateNotificationAsync(Notification notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task UpdateNotificationAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationAsync(Notification notification)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
        }
    }
}
