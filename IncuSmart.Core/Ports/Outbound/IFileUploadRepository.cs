namespace IncuSmart.Core.Ports.Outbound
{
    public interface IFileUploadRepository
    {
        Task Add(FileUpload fileUpload);

        Task<FileUpload?> FindById(Guid id);

        Task<List<FileUpload>> FindAll();

        Task<List<FileUpload>> FindByUploadedByUserId(Guid userId);
    }
}
