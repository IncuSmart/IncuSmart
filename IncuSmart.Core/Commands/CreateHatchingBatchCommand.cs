namespace IncuSmart.Core.Commands
{
    public class CreateHatchingBatchCommand
    {
        public Guid    SeasonId   { get; set; }
        public int     BatchIndex { get; set; }
        public string? Name       { get; set; }
        public int     DayStart   { get; set; }
        public int     DayEnd     { get; set; }
        public string? Notes      { get; set; }
        public List<BatchConfigItemCommand> Configs { get; set; } = new();
    }
}
