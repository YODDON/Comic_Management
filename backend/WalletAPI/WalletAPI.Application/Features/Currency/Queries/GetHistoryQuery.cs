using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SharedKernel.Responses;
using WalletAPI.DTOs;
using WalletAPI.Interfaces;

namespace WalletAPI.Application.Features.Currency.Queries
{
    public class GetHistoryQuery : IRequest<ApiResponse<CurrencyHistoryResultDto>>
    {
        public int UserId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }

    public class GetHistoryQueryHandler : IRequestHandler<GetHistoryQuery, ApiResponse<CurrencyHistoryResultDto>>
    {
        private readonly ICurrencyRepository _repository;

        public GetHistoryQueryHandler(ICurrencyRepository repository)
        {
            _repository = repository;
        }

        public async Task<ApiResponse<CurrencyHistoryResultDto>> Handle(GetHistoryQuery request, CancellationToken cancellationToken)
        {
            if (request.PageNumber < 1 || request.PageSize is < 1 or > 100)
            {
                return ApiResponse<CurrencyHistoryResultDto>.ErrorResponse(
                    "Page number must be at least 1 and page size must be between 1 and 100.",
                    400);
            }

            var (items, totalCount, balance) = await _repository.GetHistoryAsync(request.UserId, request.PageNumber, request.PageSize);
            
            var dtos = items.Select(x => new CurrencyEntryDto
            {
                Id = x.Id,
                Amount = x.Amount,
                Type = x.Type,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            }).ToList();

            return new ApiResponse<CurrencyHistoryResultDto>(new CurrencyHistoryResultDto(
                dtos,
                totalCount,
                request.PageNumber,
                request.PageSize,
                balance
            ));
        }
    }
}
