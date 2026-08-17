using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Domain.Entities;
using ComicAPI.Domain.Interfaces;
using ComicAPI.Application.Interfaces;
using MediatR;
using SharedKernel.Enums;
using SharedKernel.Responses;

namespace ComicAPI.Application.Features.Comics.Commands
{
    public class CreateComicCommand : IRequest<ApiResponse<ComicDetailDto>>
    {
        public CreateComicRequestDto Request { get; set; } = null!;
        public int UserId { get; set; }
    }

    public class CreateComicCommandHandler : IRequestHandler<CreateComicCommand, ApiResponse<ComicDetailDto>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;

        public CreateComicCommandHandler(IComicRepository comicRepository, IMapper mapper, ICloudinaryService cloudinaryService)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<ApiResponse<ComicDetailDto>> Handle(CreateComicCommand request, CancellationToken cancellationToken)
        {
            string coverUrl = request.Request.CoverImage != null
                ? await _cloudinaryService.UploadImageAsync(request.Request.CoverImage)
                : request.Request.CoverUrl!.Trim();

            var comic = new Comic
            {
                Title = request.Request.Title,
                Description = request.Request.Description ?? string.Empty,
                ThumbnailUrl = coverUrl,
                OwnerId = request.UserId,
                Author = request.Request.Author ?? string.Empty,
                UnitPrice = request.Request.UnitPrice,
                SalaryType = request.Request.SalaryType?.ToString() ?? "0",
                Status = ComicStatus.Ongoing,
                ViewCount = 0,
                Slug = !string.IsNullOrWhiteSpace(request.Request.Slug) 
                    ? request.Request.Slug 
                    : SharedKernel.Utilities.SlugGenerator.GenerateSlug(request.Request.Title)
            };

            if (request.Request.CategoryIds != null && request.Request.CategoryIds.Any())
            {
                comic.ComicCategories = request.Request.CategoryIds.Select(id => new ComicCategory { CategoryId = id }).ToList();
            }

            await _comicRepository.AddComicAsync(comic);

            var dto = _mapper.Map<ComicDetailDto>(comic);
            return new ApiResponse<ComicDetailDto>(dto, "Comic created successfully.", 201);
        }
    }
}
