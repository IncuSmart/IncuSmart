using System.ComponentModel.DataAnnotations;

namespace IncuSmart.Core.Utils
{
    public class EmailProperties
    {
        public const string SectionName = "Email";

        [Required] public string Username { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = "IncuSmart";
    }
}
