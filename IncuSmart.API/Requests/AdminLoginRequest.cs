namespace IncuSmart.API.Requests
{
    public class AdminLoginRequest
    {
        [Required(ErrorMessage = CommonConst.UsernameRequired)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = CommonConst.PasswordRequired)]
        public string Password { get; set; } = string.Empty;
    }
}
