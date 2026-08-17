using System;
using MassTransit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using SharedKernel.Enums;
using MissionAPI.DTOs;
using MissionAPI.Entities;
using MissionAPI.Interfaces;

namespace MissionAPI.Application.Features.Notifications.Commands
{
    public class UpdateNotificationCommand : IRequest<ApiResponse<NotificationDto>>
    {
        public Guid Id { get; set; }
        public UpdateNotificationDto Request { get; set; }
    }

    public class UpdateNotificationCommandHandler : IRequestHandler<UpdateNotificationCommand, ApiResponse<NotificationDto>>
    {
        private readonly INotificationRepository _notificationRepository;

        public UpdateNotificationCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<NotificationDto>> Handle(UpdateNotificationCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Request.Title) || string.IsNullOrWhiteSpace(request.Request.Body))
                return ApiResponse<NotificationDto>.ErrorResponse("Tiêu đề và nội dung không được để trống.", 400);
            var notification = await _notificationRepository.GetNotificationByIdAsync(request.Id);
            if (notification == null) return ApiResponse<NotificationDto>.ErrorResponse("Không tìm thấy thông báo.", 404);
            notification.Title = request.Request.Title.Trim();
            notification.Body = request.Request.Body.Trim();
            notification.IsActive = request.Request.IsActive;
            notification.UpdatedAt = DateTime.UtcNow;
            await _notificationRepository.UpdateNotificationAsync(notification);
            return new ApiResponse<NotificationDto>(new NotificationDto
            {
                Id = notification.Id, UserId = notification.UserId, Title = notification.Title,
                Body = notification.Body, IsRead = notification.IsRead, IsActive = notification.IsActive,
                CreatedAt = notification.CreatedAt
            }, "Notification updated successfully");
        }

    }
}
