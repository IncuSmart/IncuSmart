namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/file-uploads")]
    public class FileUploadController(IFileUploadUseCase _fileUploadUseCase) : ApiControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return FromResult(new BaseResponse<FileUploadResponse?>
                {
                    StatusCode = "400",
                    Message = "File is required",
                    Data = null
                });

            // Convert IFormFile to byte array
            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }

            var command = new UploadFileCommand
            {
                FileData = fileData,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                Description = request.Description
            };

            var result = await _fileUploadUseCase.Upload(command);
            return FromResult(new BaseResponse<FileUploadResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _fileUploadUseCase.GetById(id);
            return FromResult(new BaseResponse<FileUploadResponse?>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _fileUploadUseCase.GetAll();
            return FromResult(new BaseResponse<List<FileUploadResponse>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUploadedByUserId(Guid userId)
        {
            var result = await _fileUploadUseCase.GetByUploadedByUserId(userId);
            return FromResult(new BaseResponse<List<FileUploadResponse>>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _fileUploadUseCase.Delete(id);
            return FromResult(new BaseResponse<bool>
            {
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            });
        }
    }
}
