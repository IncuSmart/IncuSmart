namespace IncuSmart.API.Requests
{
    public class TemplateBatchItemRequest
    {
        [Required(ErrorMessage = "BatchIndex là bắt buộc")]
        public int     BatchIndex { get; set; }

        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name    { get; set; }

        [Required(ErrorMessage = "NumberOfDays là bắt buộc")]
        [Range(1, 365, ErrorMessage = "NumberOfDays phải từ 1 đến 365")]
        public int     NumberOfDays { get; set; }

        public string? Notes   { get; set; }

        public List<BatchConfigItemRequest> Configs { get; set; } = new();
    }
}
