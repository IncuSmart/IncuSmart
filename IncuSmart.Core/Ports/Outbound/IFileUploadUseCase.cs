namespace IncuSmart.Core.Ports.Outbound
{
    public interface IFileUploadUseCase
    {
        Task<ResultModel<FileUploadResponse?>> Upload(UploadFileCommand command);

        Task<ResultModel<FileUploadResponse?>> GetById(Guid id);

        Task<ResultModel<List<FileUploadResponse>>> GetAll();

        Task<ResultModel<List<FileUploadResponse>>> GetByUploadedByUserId(Guid userId);

        Task<ResultModel<bool>> Delete(Guid id);
    }
}
