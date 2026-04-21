namespace IncuSmart.Core.Responses
{
    public class FileUploadResponse
    {
        public Guid Id { get; set; }

        public string? FileName { get; set; }

        public string? FileUrl { get; set; }

        public long FileSize { get; set; }

        public string? MimeType { get; set; }
    }
}
