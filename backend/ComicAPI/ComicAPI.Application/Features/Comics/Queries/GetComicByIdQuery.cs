using System;
using System.Threading;
using System.Threading.Tasks;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Interfaces;
using MediatR;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Queries
{
    public class GetComicByIdQuery : IRequest<ApiResponse<ComicDetailDto>>
    {
        public Guid Id { get; set; }
    }

    public class GetComicByIdQueryHandler : IRequestHandler<GetComicByIdQuery, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMediator _mediator;

        public GetComicByIdQueryHandler(IComicRepository comicRepository, IMediator mediator)
        {
            _comicRepository = comicRepository;
            _mediator = mediator;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(GetComicByIdQuery request, CancellationToken cancellationToken)
        {
            var comic = await _comicRepository.GetComicByIdAsync(request.Id);
            if (comic == null)
            {
                return new ApiResponse<ComicDetailDto>(null, "Comic not found.", 404);
            }

            return await _mediator.Send(new GetComicBySlugQuery { Slug = comic.Slug }, cancellationToken);
        }
    }
}
