namespace IncuSmart.API.Requests
{
    public class UpdateHatchingSeasonTemplateRequest
    {
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public EggType? EggType { get; set; }

        public bool? IsActive { get; set; }

        // Nếu có → soft-delete batches cũ, insert mới
        public List<TemplateBatchItemRequest>? Batches { get; set; }
    }
}
