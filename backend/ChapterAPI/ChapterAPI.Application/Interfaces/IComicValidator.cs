using System;
using System.Threading.Tasks;

namespace ChapterAPI.Interfaces
{
    public interface IComicValidator
    {
        Task<bool> ExistsAsync(Guid comicId);
    }
}
