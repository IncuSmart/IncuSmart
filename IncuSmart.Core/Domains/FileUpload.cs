namespace IncuSmart.Core.Domain
{
    public class FileUpload : BaseDomain<BaseStatus>
    {
        public string? FileName { get; set; }

        public string? FileExtension { get; set; }

        public long FileSize { get; set; }

        public string? FilePath { get; set; }

        public string? FileUrl { get; set; }

        public string? MimeType { get; set; }

        public Guid? UploadedByUserId { get; set; }

        public string? Description { get; set; }
    }
}
