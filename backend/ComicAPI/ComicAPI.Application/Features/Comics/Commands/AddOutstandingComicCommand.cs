using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Features.Comics.Helpers;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;
using UserAPI.Protos;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class AddOutstandingComicCommand : IRequest<ApiResponse<ComicDetailDto>>
    {
        public CreateOutstandingRequestDto Request { get; set; } = null!;
    }

    public class AddOutstandingComicCommandHandler : IRequestHandler<AddOutstandingComicCommand, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;

        public AddOutstandingComicCommandHandler(IComicRepository comicRepository, IMapper mapper, UserService.UserServiceClient userServiceClient)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(AddOutstandingComicCommand request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.Request.ComicId);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            if (comic.Status == ComicStatus.Dropped)
            {
                return new ApiResponse<ComicDetailDto>(null, "Rejected comics cannot be marked as outstanding.", 409);
            }

            var isOutstanding = await _comicRepository.IsComicOutstandingAsync(request.Request.ComicId);
            if (isOutstanding)
            {
                var existingDto = _mapper.Map<ComicDetailDto>(comic);
                return new ApiResponse<ComicDetailDto>(existingDto, "Comic is already outstanding.", 200);
            }

            var outstanding = new Outstanding
            {
                ComicId = request.Request.ComicId,
                Priority = request.Request.Priority,
                StartDate = request.Request.StartDate ?? DateTime.UtcNow,
                EndDate = request.Request.EndDate ?? DateTime.UtcNow.AddDays(7)
            };

            await _comicRepository.AddOutstandingAsync(outstanding);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            dto.IsOutstanding = true;
            var summary = _mapper.Map<ComicSummaryDto>(comic);
            await ComicHelper.EnrichComicsWithAuthorNamesAsync(_userServiceClient, new List<ComicSummaryDto> { summary }, new List<Comic> { comic });
            dto.AuthorName = summary.AuthorName;

            return new ApiResponse<ComicDetailDto>(dto, "Comic added to outstandings.", 201);
        }
    }
}
