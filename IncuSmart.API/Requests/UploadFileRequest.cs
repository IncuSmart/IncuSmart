namespace IncuSmart.API.Requests
{
    public class UploadFileRequest
    {
        [Required(ErrorMessage = "File is required")]
        public IFormFile? File { get; set; }

        [MaxLength(500, ErrorMessage = "Description must be at most 500 characters")]
        public string? Description { get; set; }
    }
}
