using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MissionAPI.DTOs;
using SharedKernel.Responses;

namespace MissionAPI.Interfaces
{
    public interface INotificationService
    {
        Task<ApiResponse<PagedResult<NotificationDto>>> GetNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize);
        Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationDto request);
        Task<ApiResponse<IEnumerable<NotificationDto>>> GetAllNotificationsForAdminAsync();
        Task<ApiResponse<NotificationDto>> UpdateNotificationAsync(Guid id, UpdateNotificationDto request);
        Task<ApiResponse<bool>> DeleteNotificationForAdminAsync(Guid id);
        Task<ApiResponse<bool>> MarkAsReadAsync(int userId, Guid id);
        Task<ApiResponse<bool>> DeleteNotificationAsync(int userId, Guid id);
    }
}
