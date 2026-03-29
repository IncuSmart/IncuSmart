namespace IncuSmart.Core.Commands
{
    public class UpdateHatchingBatchCommand
    {
        public Guid       Id            { get; set; }
        public string?    Name          { get; set; }
        public int?       DayStart      { get; set; }
        public int?       DayEnd        { get; set; }
        public DateTime?  ActualStartAt { get; set; }
        public DateTime?  ActualEndAt   { get; set; }
        public string?    Status        { get; set; }
        // Nếu có: soft-delete configs cũ, insert mới
        public List<BatchConfigItemCommand>? Configs { get; set; }
    }
}
