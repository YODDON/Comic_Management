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
    public class GetNotificationsQuery : IRequest<ApiResponse<PagedResult<NotificationDto>>>
    {
        public int UserId { get; set; }
        public bool? IsRead { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, ApiResponse<PagedResult<NotificationDto>>>
    {
        private readonly INotificationRepository _notificationRepository;

        public GetNotificationsQueryHandler(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task<ApiResponse<PagedResult<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var (notifications, totalCount) = await _notificationRepository.GetNotificationsAsync(request.UserId, request.IsRead, request.PageIndex, request.PageSize);
            
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

            var pagedResult = new PagedResult<NotificationDto>(dtos, totalCount, request.PageIndex, request.PageSize);
            return new ApiResponse<PagedResult<NotificationDto>>(pagedResult, "Notifications retrieved successfully");
        }

    }
}
