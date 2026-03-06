namespace IncuSmart.Core.Domain
{
    public class User : BaseDomain<BaseStatus>
    {
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Phone { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
