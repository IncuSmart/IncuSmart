namespace IncuSmart.Core.Commands
{
    public class CreateUserCommand
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserCommand
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateProfileCommand
    {
        public Guid Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class UpdateCustomerProfileCommand
    {
        public Guid CustomerId { get; set; }
        public string? Address { get; set; }
    }

    public class ChangePasswordCommand
    {
        public Guid UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordCommand
    {
        public Guid UserId { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }
}
