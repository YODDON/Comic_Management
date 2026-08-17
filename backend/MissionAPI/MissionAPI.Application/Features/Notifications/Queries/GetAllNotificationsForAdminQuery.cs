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

namespace MissionAPI.Application.Features.Notifications.Queries
{
    public class GetAllNotificationsForAdminQuery : IRequest<ApiResponse<IEnumerable<NotificationDto>>>
    {
    }

    public class GetAllNotificationsForAdminQueryHandler : IRequestHandler<GetAllNotificationsForAdminQuery, ApiResponse<IEnumerable<NotificationDto>>>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetAllNotificationsForAdminQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<IEnumerable<NotificationDto>>> Handle(GetAllNotificationsForAdminQuery request, CancellationToken cancellationToken)
        {
            var items = await _notificationRepository.GetAllNotificationsForAdminAsync();
            var dtos = items.Select(n => new NotificationDto
            {
                Id = n.Id, UserId = n.UserId, Title = n.Title, Body = n.Body,
                IsRead = n.IsRead, IsActive = n.IsActive, CreatedAt = n.CreatedAt
            });
            return new ApiResponse<IEnumerable<NotificationDto>>(dtos, "Notifications retrieved successfully");
        }

    }
}
