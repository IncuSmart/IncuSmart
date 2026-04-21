using Microsoft.Extensions.Configuration;

namespace IncuSmart.Infra.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IConfiguration _configuration;
        private const string DEFAULT_SUBDIRECTORY = "uploads";
        private const long MAX_FILE_SIZE = 100 * 1024 * 1024; // 100 MB

        private static readonly List<string> ALLOWED_EXTENSIONS = new()
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".zip", ".rar", ".7z",
            ".txt", ".csv", ".json", ".xml"
        };

        public FileUploadService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> SaveFileAsync(byte[] fileData, string fileName, string? subdirectory = null)
        {
            if (fileData == null || fileData.Length == 0)
                throw new ArgumentException("File data cannot be null or empty");

            if (fileData.Length > MAX_FILE_SIZE)
                throw new ArgumentException($"File size exceeds maximum allowed size of {MAX_FILE_SIZE / (1024 * 1024)} MB");

            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (!ALLOWED_EXTENSIONS.Contains(extension))
                throw new ArgumentException($"File extension '{extension}' is not allowed");

            string baseUploadPath = _configuration["FileUpload:UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            
            string uploadDir = Path.Combine(baseUploadPath, DEFAULT_SUBDIRECTORY);
            if (!string.IsNullOrEmpty(subdirectory))
            {
                uploadDir = Path.Combine(uploadDir, subdirectory);
            }

            if (!Directory.Exists(uploadDir))
                Directory.CreateDirectory(uploadDir);

            string newFileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadDir, newFileName);

            await File.WriteAllBytesAsync(filePath, fileData);

            return newFileName;
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            string baseUploadPath = _configuration["FileUpload:UploadPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string fullPath = Path.Combine(baseUploadPath, DEFAULT_SUBDIRECTORY, filePath);
            
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        public string GetFileUrl(string fileName, string? subdirectory = null)
        {
            string baseUrl = _configuration["FileUpload:BaseUrl"] ?? "";
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = "/";
            }

            if (string.IsNullOrEmpty(subdirectory))
            {
                return $"{baseUrl}{DEFAULT_SUBDIRECTORY}/{fileName}";
            }

            return $"{baseUrl}{DEFAULT_SUBDIRECTORY}/{subdirectory}/{fileName}";
        }
    }
}
