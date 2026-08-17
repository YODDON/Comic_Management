using System.Threading.Tasks;
using MissionAPI.Data;
using MissionAPI.Entities;
using MissionAPI.Interfaces;

namespace MissionAPI.Repositories
{
    public class UploadRepository : IUploadRepository
    {
        private readonly MissionDbContext _context;

        public UploadRepository(MissionDbContext context)
        {
            _context = context;
        }

        public async Task<Upload> CreateUploadRecordAsync(Upload upload)
        {
            _context.Uploads.Add(upload);
            await _context.SaveChangesAsync();
            return upload;
        }
    }
}
