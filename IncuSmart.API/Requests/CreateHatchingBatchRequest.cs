namespace IncuSmart.API.Requests
{
    public class CreateHatchingBatchRequest
    {
        [Required(ErrorMessage = "SeasonId là bắt buộc")]
        public Guid SeasonId { get; set; }

        [Required(ErrorMessage = "BatchIndex là bắt buộc")]
        public int BatchIndex { get; set; }

        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "DayStart là bắt buộc")]
        public int DayStart { get; set; }

        [Required(ErrorMessage = "DayEnd là bắt buộc")]
        public int DayEnd { get; set; }

        public string? Notes { get; set; }

        public List<BatchConfigItemRequest> Configs { get; set; } = new();
    }
}
