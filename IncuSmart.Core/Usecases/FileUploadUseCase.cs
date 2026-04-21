namespace IncuSmart.Core.Usecases
{
    public class FileUploadUseCase : IFileUploadUseCase
    {
        private readonly IFileUploadRepository _fileUploadRepository;
        private readonly IFileUploadService _fileUploadService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<FileUploadUseCase> _logger;

        public FileUploadUseCase(
            IFileUploadRepository fileUploadRepository,
            IFileUploadService fileUploadService,
            IUnitOfWork unitOfWork,
            ILogger<FileUploadUseCase> logger)
        {
            _fileUploadRepository = fileUploadRepository;
            _fileUploadService = fileUploadService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<FileUploadResponse?>> Upload(UploadFileCommand command)
        {
            if (command.FileData == null || command.FileData.Length == 0)
                return ResultModelUtils.FillResult<FileUploadResponse?>("400", "File is required", null);

            if (string.IsNullOrEmpty(command.FileName))
                return ResultModelUtils.FillResult<FileUploadResponse?>("400", "File name is required", null);

            await _unitOfWork.BeginAsync();
            try
            {
                ///save file to wwwroot
                string fileName = await _fileUploadService.SaveFileAsync(command.FileData, command.FileName);
                string fileUrl = _fileUploadService.GetFileUrl(fileName);

                var fileUpload = new FileUpload
                {
                    Id = Guid.NewGuid(),
                    FileName = command.FileName,
                    FileExtension = Path.GetExtension(command.FileName),
                    FileSize = command.FileSize,
                    FilePath = fileName,
                    FileUrl = fileUrl,
                    MimeType = command.ContentType,
                    UploadedByUserId = command.UploadedByUserId,
                    Description = command.Description,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "SYSTEM",
                };

                await _fileUploadRepository.Add(fileUpload);
                await _unitOfWork.CommitAsync();

                var response = new FileUploadResponse
                {
                    Id = fileUpload.Id,
                    FileName = fileUpload.FileName,
                    FileUrl = fileUpload.FileUrl,
                    FileSize = fileUpload.FileSize,
                    MimeType = fileUpload.MimeType,
                };

                return ResultModelUtils.FillResult<FileUploadResponse?>("200", "File uploaded successfully", response);
            }
            catch (ArgumentException ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogWarning(ex, "Invalid file upload");
                return ResultModelUtils.FillResult<FileUploadResponse?>("400", ex.Message, null);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error uploading file");
                return ResultModelUtils.FillResult<FileUploadResponse?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<FileUploadResponse?>> GetById(Guid id)
        {
            var fileUpload = await _fileUploadRepository.FindById(id);
            if (fileUpload == null)
                return ResultModelUtils.FillResult<FileUploadResponse?>("404", "File not found", null);

            var response = new FileUploadResponse
            {
                Id = fileUpload.Id,
                FileName = fileUpload.FileName,
                FileUrl = fileUpload.FileUrl,
                FileSize = fileUpload.FileSize,
                MimeType = fileUpload.MimeType,
            };

            return ResultModelUtils.FillResult<FileUploadResponse?>("200", "Success", response);
        }

        public async Task<ResultModel<List<FileUploadResponse>>> GetAll()
        {
            var list = await _fileUploadRepository.FindAll();
            var responses = list.Select(x => new FileUploadResponse
            {
                Id = x.Id,
                FileName = x.FileName,
                FileUrl = x.FileUrl,
                FileSize = x.FileSize,
                MimeType = x.MimeType,
            }).ToList();

            return ResultModelUtils.FillResult<List<FileUploadResponse>>("200", "Success", responses);
        }

        public async Task<ResultModel<List<FileUploadResponse>>> GetByUploadedByUserId(Guid userId)
        {
            var list = await _fileUploadRepository.FindByUploadedByUserId(userId);
            var responses = list.Select(x => new FileUploadResponse
            {
                Id = x.Id,
                FileName = x.FileName,
                FileUrl = x.FileUrl,
                FileSize = x.FileSize,
                MimeType = x.MimeType,
            }).ToList();

            return ResultModelUtils.FillResult<List<FileUploadResponse>>("200", "Success", responses);
        }

        public async Task<ResultModel<bool>> Delete(Guid id)
        {
            var fileUpload = await _fileUploadRepository.FindById(id);
            if (fileUpload == null)
                return ResultModelUtils.FillResult<bool>("404", "File not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                // Delete physical file
                _fileUploadService.DeleteFile(fileUpload.FilePath ?? "");

                // Soft delete in database
                fileUpload.DeletedAt = DateTime.UtcNow;
                fileUpload.DeletedBy = "SYSTEM";
                fileUpload.UpdatedAt = DateTime.UtcNow;
                fileUpload.UpdatedBy = "SYSTEM";

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "File deleted successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting file {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
