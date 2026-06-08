namespace IncuSmart.API.Requests
{
    public class RegisterRequest
    {
        [NotNull]
        [MinLength(8, ErrorMessage = "Username must be at least 8 characters long")]
        [MaxLength(15, ErrorMessage = "Username must be at most 15 characters long")]
        public string Username { get; set; } = default!;
        [NotNull]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        [MaxLength(20, ErrorMessage = "Password must be at most 20 characters long")]
        public string Password { get; set; } = default!;

        [NotNull]
        [MinLength(3, ErrorMessage = "Full name must be at least 3 characters long")]
        [MaxLength(50, ErrorMessage = "Full name must be at most 50 characters long")]
        public string FullName { get; set; } = default!;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ")]
        [MaxLength(100, ErrorMessage = CommonConst.EmailMaxLength)]
        public string Email { get; set; } = string.Empty;

        [NotNull]
        [RegularExpression(@"^(0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$",
        ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; } = string.Empty;
    }
}
