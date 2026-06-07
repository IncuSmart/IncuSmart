namespace IncuSmart.Core.Commands
{
    public class UpdateHatchingSeasonTemplateCommand
    {
        public Guid    Id          { get; set; }
        public string? Name        { get; set; }
        public string? Description { get; set; }
        public EggType? EggType     { get; set; }
        public bool?   IsActive    { get; set; }
        // Nếu có: soft-delete batches cũ, insert mới
        public List<TemplateBatchItemCommand>? Batches { get; set; }
    }
}
