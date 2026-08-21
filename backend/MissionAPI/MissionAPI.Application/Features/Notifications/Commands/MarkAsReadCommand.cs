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
    public class MarkAsReadCommand : IRequest<ApiResponse<bool>>
    {
        public int UserId { get; set; }
        public Guid Id { get; set; }
    }

    public class MarkAsReadCommandHandler : IRequestHandler<MarkAsReadCommand, ApiResponse<bool>>
    {
        private readonly INotificationRepository _notificationRepository;

        public MarkAsReadCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<bool>> Handle(MarkAsReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(request.Id);
            if (notification == null)
            {
                return new ApiResponse<bool>(false, "Notification not found", 404);
            }

            if (notification.UserId != 0 && notification.UserId != request.UserId)
            {
                return new ApiResponse<bool>(false, "Forbidden", 403);
            }

            notification.IsRead = true;
            await _notificationRepository.UpdateNotificationAsync(notification);

            return new ApiResponse<bool>(true, "Notification marked as read");
        }

    }
}
