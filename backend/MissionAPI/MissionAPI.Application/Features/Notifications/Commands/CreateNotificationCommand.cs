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
    public class CreateNotificationCommand : IRequest<ApiResponse<NotificationDto>>
    {
        public CreateNotificationDto Request { get; set; }
    }

    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, ApiResponse<NotificationDto>>
    {
        private readonly INotificationRepository _notificationRepository;

        public CreateNotificationCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<NotificationDto>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Request.Title) || string.IsNullOrWhiteSpace(request.Request.Body))
                return ApiResponse<NotificationDto>.ErrorResponse("Tiêu đề và nội dung thông báo không được để trống.", 400);

            var notification = new Notification
            {
                // Use Guid.Empty if not provided (broadcast)
                UserId = request.Request.UserId ?? 0,
                Title = request.Request.Title.Trim(),
                Body = request.Request.Body.Trim(),
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

    }
}
