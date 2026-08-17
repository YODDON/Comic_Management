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
    public class DeleteNotificationCommand : IRequest<ApiResponse<bool>>
    {
        public int UserId { get; set; }
        public Guid Id { get; set; }
    }

    public class DeleteNotificationCommandHandler : IRequestHandler<DeleteNotificationCommand, ApiResponse<bool>>
    {
        private readonly INotificationRepository _notificationRepository;

        public DeleteNotificationCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(request.Id);
            if (notification == null)
            {
                return new ApiResponse<bool>(false, "Notification not found", 404);
            }

            if (notification.UserId == 0)
            {
                return new ApiResponse<bool>(false, "Cannot delete broadcast notification", 403);
            }

            if (notification.UserId != request.UserId)
            {
                return new ApiResponse<bool>(false, "Forbidden", 403);
            }

            await _notificationRepository.DeleteNotificationAsync(notification);
            return new ApiResponse<bool>(true, "Notification deleted successfully");
        }

    }
}
