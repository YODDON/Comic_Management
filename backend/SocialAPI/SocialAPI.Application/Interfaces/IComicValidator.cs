using System;
using System.Threading.Tasks;

namespace SocialAPI.Interfaces
{
    public interface IComicValidator
    {
        Task<bool> ExistsAsync(Guid comicId);
    }
}
