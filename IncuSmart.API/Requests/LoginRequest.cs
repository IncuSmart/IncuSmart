namespace IncuSmart.API.Requests
{
    public class LoginRequest
    {
        [NotNull]
        public string Username { get; set; } = default!;
        [NotNull]
        public string Password { get; set; } = default!;
    }
}
