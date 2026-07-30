using System.Threading.Tasks;
using MissionAPI.Entities;

namespace MissionAPI.Interfaces
{
    public interface IUploadRepository
    {
        Task<Upload> CreateUploadRecordAsync(Upload upload);
    }
}
