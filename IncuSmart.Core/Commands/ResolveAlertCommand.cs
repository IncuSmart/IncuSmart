namespace IncuSmart.Core.Commands
{
    public class ResolveAlertCommand
    {
        public Guid Id { get; set; }
        public string? Message { get; set; }
        public AlertResolvedBy ResolvedBy { get; set; } = AlertResolvedBy.MANUAL;
        public string? UpdatedBy { get; set; }
    }
}
