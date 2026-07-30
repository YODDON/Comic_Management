using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ComicAPI.Entities;
using SharedKernel.Enums;

namespace ComicAPI.Interfaces
{
    public interface IComicRepository
    {
        Task<(List<Comic> Items, int TotalCount)> GetComicsAsync(int pageNumber, int pageSize, string? search, List<Guid>? categoryIds, ComicStatus? status);
        Task<List<Comic>> GetHotComicsAsync(int limit);
        Task<List<Comic>> GetOutstandingComicsAsync(int limit);
        Task<(List<Comic> Items, int TotalCount)> GetOutstandingComicsAsync(int pageNumber, int pageSize);
        Task<List<Comic>> GetLastCompletedComicsAsync(int limit);
        Task<Comic?> GetComicBySlugAsync(string slug);
        Task<bool> SlugExistsAsync(string slug, Guid? excludedComicId = null);
        Task<Comic?> GetComicByIdAsync(System.Guid id);
        Task<(List<Comic> Items, int TotalCount)> GetComicsByOwnerAsync(int ownerId, int pageNumber, int pageSize);
        Task<(List<Comic> Items, int TotalCount)> GetComicsByIdsAsync(List<System.Guid> ids, int pageNumber, int pageSize);
        Task AddComicAsync(Comic comic);
        Task UpdateComicAsync(Comic comic, IReadOnlyCollection<Guid>? categoryIds = null);
        Task<bool> IncrementViewCountAsync(System.Guid comicId);
        Task<bool> IsComicOutstandingAsync(System.Guid comicId);
        Task AddOutstandingAsync(Outstanding outstanding);
        Task RemoveOutstandingAsync(System.Guid comicId);
    }
}
