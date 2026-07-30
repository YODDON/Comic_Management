using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MissionAPI.Entities;

namespace MissionAPI.Interfaces
{
    public interface INotificationRepository
    {
        Task<(IEnumerable<Notification> Notifications, int TotalCount)> GetNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize);
        Task<IEnumerable<Notification>> GetAllNotificationsForAdminAsync();
        Task<Notification?> GetNotificationByIdAsync(Guid id);
        Task<Notification> CreateNotificationAsync(Notification notification);
        Task UpdateNotificationAsync(Notification notification);
        Task DeleteNotificationAsync(Notification notification);
    }
}
