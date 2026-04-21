namespace IncuSmart.Core.Ports.Outbound
{
    public interface IFileUploadService
    {
        Task<string> SaveFileAsync(byte[] fileData, string fileName, string? subdirectory = null);

        void DeleteFile(string filePath);

        string GetFileUrl(string fileName, string? subdirectory = null);
    }
}
