using IncuSmart.Core.Enums;

namespace IncuSmart.Core.Domains
{
    public class Customer : BaseDomain<BaseStatus>
    {
        public Guid UserId { get; set; }

        public string? DeviceToken { get; set; }

        public string? PinNumberHash { get; set; }

        public string? Address { get; set; }
    }
}
