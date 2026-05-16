namespace IncuSmart.API.Requests
{
    public class CreateUserRequest
    {
        [Required(ErrorMessage = CommonConst.UsernameRequired)]
        [MinLength(3, ErrorMessage = CommonConst.UsernameMinLength)]
        [MaxLength(50, ErrorMessage = CommonConst.UsernameMaxLength)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = CommonConst.PasswordRequired)]
        [MinLength(6, ErrorMessage = CommonConst.PasswordMinLength)]
        [MaxLength(255, ErrorMessage = CommonConst.PasswordMaxLength)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = CommonConst.FullNameRequired)]
        [MinLength(2, ErrorMessage = CommonConst.FullNameMinLength)]
        [MaxLength(100, ErrorMessage = CommonConst.FullNameMaxLength)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = CommonConst.EmailMaxLength)]
        public string? Email { get; set; }

        [Required(ErrorMessage = CommonConst.PhoneRequired)]
        [MinLength(9, ErrorMessage = CommonConst.PhoneMinLength)]
        [MaxLength(20, ErrorMessage = CommonConst.PhoneMaxLength)]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = CommonConst.RoleRequired)]
        [MaxLength(20, ErrorMessage = CommonConst.RoleMaxLength)]
        public string Role { get; set; } = string.Empty;
    }
}
