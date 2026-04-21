using IncuSmart.Infra.Persistences.Entities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace IncuSmart.Infra.Persistences.Repositories
{
    public class FileUploadRepository : IFileUploadRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public FileUploadRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Add(FileUpload fileUpload)
        {
            FileUploadEntity entity = fileUpload.Adapt<FileUploadEntity>();
            await _dbContext.FileUploads.AddAsync(entity);
        }

        public async Task<FileUpload?> FindById(Guid id)
        {
            FileUploadEntity? entity = await _dbContext.FileUploads
                .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
            
            return entity?.Adapt<FileUpload>();
        }

        public async Task<List<FileUpload>> FindAll()
        {
            return (await _dbContext.FileUploads
                .Where(x => x.DeletedAt == null)
                .ToListAsync())
                .Adapt<List<FileUpload>>();
        }

        public async Task<List<FileUpload>> FindByUploadedByUserId(Guid userId)
        {
            return (await _dbContext.FileUploads
                .Where(x => x.UploadedByUserId == userId && x.DeletedAt == null)
                .ToListAsync())
                .Adapt<List<FileUpload>>();
        }
    }
}
