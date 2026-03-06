using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("users")]
    public class UserEntity : BaseEntity<BaseStatus>
    {
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string Phone { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
