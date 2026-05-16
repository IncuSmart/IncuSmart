namespace IncuSmart.Core.Commands
{
    public class UpdateHatchingSeasonStatusCommand
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
