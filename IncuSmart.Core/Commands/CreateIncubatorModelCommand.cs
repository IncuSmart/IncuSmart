namespace IncuSmart.Core.Commands
{
    public class CreateIncubatorModelCommand
    {
        public string? ModelCode { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<ModelConfigItemCommand> Configs { get; set; } = [];
    }

}
