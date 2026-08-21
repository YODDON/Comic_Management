using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class ToggleOutstandingComicCommand : IRequest<ApiResponse<ComicDetailDto>>
    {
        public CreateOutstandingRequestDto Request { get; set; } = null!;
    }

    public class ToggleOutstandingComicCommandHandler : IRequestHandler<ToggleOutstandingComicCommand, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;

        public ToggleOutstandingComicCommandHandler(IComicRepository comicRepository, IMapper mapper)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(ToggleOutstandingComicCommand request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.Request.ComicId);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot change outstanding status.", 409);
            }

            var isOutstanding = await _comicRepository.IsComicOutstandingAsync(request.Request.ComicId);
            if (isOutstanding)
            {
                await _comicRepository.RemoveOutstandingAsync(request.Request.ComicId);
            }
            else
            {
                await _comicRepository.AddOutstandingAsync(new Outstanding
                {
                    ComicId = request.Request.ComicId,
                    Priority = request.Request.Priority,
                    StartDate = request.Request.StartDate ?? DateTime.UtcNow,
                    EndDate = request.Request.EndDate ?? DateTime.UtcNow.AddDays(7)
                });
            }

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = !isOutstanding;
            return new ApiResponse<ComicDetailDto>(
                dto,
                dto.IsOutstanding ? "Comic marked as outstanding." : "Comic removed from outstandings.",
                200);
        }
    }
}
