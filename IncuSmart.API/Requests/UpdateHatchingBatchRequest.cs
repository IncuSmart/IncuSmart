namespace IncuSmart.API.Requests
{
    public class UpdateHatchingBatchRequest
    {
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        public int?      DayStart      { get; set; }
        public int?      DayEnd        { get; set; }
        public DateTime? ActualStartAt { get; set; }
        public DateTime? ActualEndAt   { get; set; }
        public string?   Status        { get; set; }

        // Nếu có → soft-delete configs cũ, insert mới
        public List<BatchConfigItemRequest>? Configs { get; set; }
    }
}
