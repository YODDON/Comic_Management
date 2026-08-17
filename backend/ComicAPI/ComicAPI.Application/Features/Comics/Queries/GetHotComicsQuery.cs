using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using ComicAPI.Application.DTOs;
using ComicAPI.Application.Features.Comics.Helpers;
using ComicAPI.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using SharedKernel.Responses;
using UserAPI.Protos;

namespace ComicAPI.Application.Features.Comics.Queries
{
    public class GetHotComicsQuery : IRequest<ApiResponse<List<ComicSummaryDto>>>
    {
        public int Limit { get; set; }
    }

    public class GetHotComicsQueryHandler : IRequestHandler<GetHotComicsQuery, ApiResponse<List<ComicSummaryDto>>>
    {
        private readonly IComicRepository _comicRepository;
        private readonly IMapper _mapper;
        private readonly UserService.UserServiceClient _userServiceClient;
        private readonly IDistributedCache _cache;

        public GetHotComicsQueryHandler(IComicRepository comicRepository, IMapper mapper, UserService.UserServiceClient userServiceClient, IDistributedCache cache)
        {
            _comicRepository = comicRepository;
            _mapper = mapper;
            _userServiceClient = userServiceClient;
            _cache = cache;
        }

        public async Task<ApiResponse<List<ComicSummaryDto>>> Handle(GetHotComicsQuery request, CancellationToken cancellationToken)
        {
            var limit = request.Limit <= 0 ? 10 : (request.Limit > 50 ? 50 : request.Limit);
            
            string cacheKey = $"HotComics_{limit}";
            var cachedData = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var cachedDtos = JsonSerializer.Deserialize<List<ComicSummaryDto>>(cachedData);
                if (cachedDtos != null) return new ApiResponse<List<ComicSummaryDto>>(cachedDtos, "Hot comics retrieved from cache.", 200);
            }

            var comics = await _comicRepository.GetHotComicsAsync(limit);
            var dtos = _mapper.Map<List<ComicSummaryDto>>(comics);
            await ComicHelper.EnrichComicsWithAuthorNamesAsync(_userServiceClient, dtos, comics);
            
            var serializedDtos = JsonSerializer.Serialize(dtos);
            await _cache.SetStringAsync(cacheKey, serializedDtos, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) }, cancellationToken);

            return new ApiResponse<List<ComicSummaryDto>>(dtos, "Hot comics retrieved.", 200);
        }
    }
}
