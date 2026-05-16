namespace IncuSmart.API.Requests
{
    public class UpdateHatchingSeasonTemplateRequest
    {
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        [Range(1, 365, ErrorMessage = "TotalDays phải từ 1 đến 365")]
        public int? TotalDays { get; set; }

        [MaxLength(50, ErrorMessage = "EggType không được vượt quá 50 ký tự")]
        public string? EggType { get; set; }

        public bool? IsActive { get; set; }

        // Nếu có → soft-delete batches cũ, insert mới
        public List<TemplateBatchItemRequest>? Batches { get; set; }
    }
}
