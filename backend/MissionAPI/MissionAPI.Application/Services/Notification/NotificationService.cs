using System;
using System.Linq;
using System.Threading.Tasks;
using MissionAPI.DTOs;
using MissionAPI.Entities;
using MissionAPI.Interfaces;
using SharedKernel.Responses;

namespace MissionAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<PagedResult<NotificationDto>>> GetNotificationsAsync(int userId, bool? isRead, int pageIndex, int pageSize)
        {
            var (notifications, totalCount) = await _notificationRepository.GetNotificationsAsync(userId, isRead, pageIndex, pageSize);
            
            var dtos = notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Body = n.Body,
                IsRead = n.IsRead,
                IsActive = n.IsActive,
                CreatedAt = n.CreatedAt
            }).ToList();

            var pagedResult = new PagedResult<NotificationDto>(dtos, totalCount, pageIndex, pageSize);
            return new ApiResponse<PagedResult<NotificationDto>>(pagedResult, "Notifications retrieved successfully");
        }

        public async Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                return ApiResponse<NotificationDto>.ErrorResponse("Tiêu đề và nội dung thông báo không được để trống.", 400);

            var notification = new Notification
            {
                // Use Guid.Empty if not provided (broadcast)
                UserId = request.UserId ?? 0,
                Title = request.Title.Trim(),
                Body = request.Body.Trim(),
                IsRead = false
                ,IsActive = true
            };

            await _notificationRepository.CreateNotificationAsync(notification);

            var dto = new NotificationDto
            {
                Id = notification.Id,
                UserId = notification.UserId,
                Title = notification.Title,
                Body = notification.Body,
                IsRead = notification.IsRead,
                IsActive = notification.IsActive,
                CreatedAt = notification.CreatedAt
            };

            return new ApiResponse<NotificationDto>(dto, "Notification created successfully");
        }

        public async Task<ApiResponse<IEnumerable<NotificationDto>>> GetAllNotificationsForAdminAsync()
        {
            var items = await _notificationRepository.GetAllNotificationsForAdminAsync();
            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id, UserId = n.UserId, Title = n.Title, Body = n.Body,
                IsRead = n.IsRead, IsActive = n.IsActive, CreatedAt = n.CreatedAt
            });
            return new ApiResponse<IEnumerable<NotificationDto>>(dtos, "Notifications retrieved successfully");
        }

        public async Task<ApiResponse<NotificationDto>> UpdateNotificationAsync(Guid id, UpdateNotificationDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
                return ApiResponse<NotificationDto>.ErrorResponse("Tiêu đề và nội dung không được để trống.", 400);
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null) return ApiResponse<NotificationDto>.ErrorResponse("Không tìm thấy thông báo.", 404);
            notification.Title = request.Title.Trim();
            notification.Body = request.Body.Trim();
            notification.IsActive = request.IsActive;
            notification.UpdatedAt = DateTime.UtcNow;
            await _notificationRepository.UpdateNotificationAsync(notification);
            return new ApiResponse<NotificationDto>(new NotificationDto
            {
                Id = notification.Id, UserId = notification.UserId, Title = notification.Title,
                Body = notification.Body, IsRead = notification.IsRead, IsActive = notification.IsActive,
                CreatedAt = notification.CreatedAt
            }, "Notification updated successfully");
        }

        public async Task<ApiResponse<bool>> DeleteNotificationForAdminAsync(Guid id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null) return new ApiResponse<bool>(false, "Không tìm thấy thông báo.", 404);
            await _notificationRepository.DeleteNotificationAsync(notification);
            return new ApiResponse<bool>(true, "Notification deleted successfully");
        }

        public async Task<ApiResponse<bool>> MarkAsReadAsync(int userId, Guid id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return new ApiResponse<bool>(false, "Notification not found", 404);
            }

            if (notification.UserId != 0 && notification.UserId != userId)
            {
                return new ApiResponse<bool>(false, "Forbidden", 403);
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateNotificationAsync(notification);

            return new ApiResponse<bool>(true, "Notification marked as read");
        }

        public async Task<ApiResponse<bool>> DeleteNotificationAsync(int userId, Guid id)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(id);
            if (notification == null)
            {
                return new ApiResponse<bool>(false, "Notification not found", 404);
            }

            if (notification.UserId == 0)
            {
                return new ApiResponse<bool>(false, "Cannot delete broadcast notification", 403);
            }

            if (notification.UserId != userId)
            {
                return new ApiResponse<bool>(false, "Forbidden", 403);
            }

            await _notificationRepository.DeleteNotificationAsync(notification);
            return new ApiResponse<bool>(true, "Notification deleted successfully");
        }
    }
}
