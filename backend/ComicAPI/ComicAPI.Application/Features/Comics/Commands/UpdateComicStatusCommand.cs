using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class UpdateComicStatusCommand : IRequest<ApiResponse<ComicDetailDto>>
    {
        public Guid ComicId { get; set; }
        public UpdateComicStatusRequestDto Request { get; set; } = null!;
    }

    public class UpdateComicStatusCommandHandler : IRequestHandler<UpdateComicStatusCommand, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;

        public UpdateComicStatusCommandHandler(IComicRepository comicRepository, IMapper mapper)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(UpdateComicStatusCommand request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.ComicId);
            if (comic == null) return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);

            if (comic.Status == ComicStatus.Dropped && request.Request.Status != ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot be approved again.", 409);
            }

            comic.Status = request.Request.Status;
            await _comicRepository.UpdateComicAsync(comic);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic status updated successfully.", 200);
        }
    }
}
