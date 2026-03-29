namespace IncuSmart.Core.Commands
{
    public class CreateHatchingSeasonTemplateCommand
    {
        public Guid?   CustomerId     { get; set; }
        public string  Name           { get; set; } = string.Empty;
        public string? Description    { get; set; }
        public int     TotalDays      { get; set; }
        public string? EggType        { get; set; }
        public string  CreatedByType  { get; set; } = string.Empty;  // CUSTOMER | TECHNICIAN
        public List<TemplateBatchItemCommand> Batches { get; set; } = new();
    }
}
