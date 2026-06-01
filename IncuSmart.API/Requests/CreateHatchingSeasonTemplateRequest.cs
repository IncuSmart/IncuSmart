namespace IncuSmart.API.Requests
{
    public class CreateHatchingSeasonTemplateRequest
    {
        // Null = template public do Technician tạo
        public Guid? CustomerId { get; set; }

        [Required(ErrorMessage = "Name là bắt buộc")]
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [MaxLength(50, ErrorMessage = "EggType không được vượt quá 50 ký tự")]
        public string? EggType { get; set; }

        // CUSTOMER | TECHNICIAN
        [Required(ErrorMessage = "CreatedByType là bắt buộc")]
        [MaxLength(20, ErrorMessage = "CreatedByType không được vượt quá 20 ký tự")]
        public string CreatedByType { get; set; } = string.Empty;

        public List<TemplateBatchItemRequest> Batches { get; set; } = new();
    }
}
