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
    public class DeleteNotificationForAdminCommand : IRequest<ApiResponse<bool>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteNotificationForAdminCommandHandler : IRequestHandler<DeleteNotificationForAdminCommand, ApiResponse<bool>>
    {
        private readonly INotificationRepository _notificationRepository;

        public DeleteNotificationForAdminCommandHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<bool>> Handle(DeleteNotificationForAdminCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notificationRepository.GetNotificationByIdAsync(request.Id);
            if (notification == null) return new ApiResponse<bool>(false, "Không tìm thấy thông báo.", 404);
            await _notificationRepository.DeleteNotificationAsync(notification);
            return new ApiResponse<bool>(true, "Notification deleted successfully");
        }

    }
}
