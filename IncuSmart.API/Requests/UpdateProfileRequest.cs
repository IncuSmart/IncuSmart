namespace IncuSmart.API.Requests
{
    public class UpdateProfileRequest
    {
        [MinLength(2, ErrorMessage = "FullName must be at least 2 characters")]
        [MaxLength(100, ErrorMessage = "FullName must be at most 100 characters")]
        public string? FullName { get; set; }

        [MaxLength(100, ErrorMessage = "Email must be at most 100 characters")]
        public string? Email { get; set; }

        [MinLength(9, ErrorMessage = "Phone must be at least 9 characters")]
        [MaxLength(20, ErrorMessage = "Phone must be at most 20 characters")]
        public string? Phone { get; set; }
    }
}
