namespace IncuSmart.Core.Commands
{
    public class CreateHatchingSeasonCommand
    {
        public Guid     IncubatorId { get; set; }
        public Guid?    TemplateId  { get; set; }
        public string?  Name        { get; set; }
        public EggType? EggType     { get; set; }
        public DateOnly StartDate   { get; set; }
        public int?     TotalEggs   { get; set; }
        public string?  Notes       { get; set; }
    }
}
