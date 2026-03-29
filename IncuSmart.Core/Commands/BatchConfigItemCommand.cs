namespace IncuSmart.Core.Commands
{
    // Nested command dùng chung cho cả Template và Season batch configs
    public class BatchConfigItemCommand
    {
        public Guid     ConfigId    { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue    { get; set; }
        public decimal? MaxValue    { get; set; }
    }
}
