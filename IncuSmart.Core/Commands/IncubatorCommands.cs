namespace IncuSmart.Core.Commands
{
    public class CreateIncubatorCommand
    {
        public Guid ModelId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateConfigInstanceItemCommand
    {
        public Guid ConfigInstanceId { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class UpdateConfigInstancesCommand
    {
        public Guid IncubatorId { get; set; }
        public List<UpdateConfigInstanceItemCommand> Items { get; set; } = [];
    }
}
