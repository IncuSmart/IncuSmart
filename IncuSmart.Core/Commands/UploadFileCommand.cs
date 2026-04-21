namespace IncuSmart.Core.Commands
{
    public class UploadFileCommand
    {
        public byte[]? FileData { get; set; }

        public string? FileName { get; set; }

        public string? ContentType { get; set; }

        public long FileSize { get; set; }

        public Guid? UploadedByUserId { get; set; }

        public string? Description { get; set; }
    }
}
