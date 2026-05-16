namespace IncuSmart.API.Requests
{
    public class CreateHatchingSeasonRequest
    {
        [Required(ErrorMessage = "IncubatorId là bắt buộc")]
        public Guid IncubatorId { get; set; }

        public Guid? TemplateId { get; set; }

        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        [MaxLength(50, ErrorMessage = "EggType không được vượt quá 50 ký tự")]
        public string? EggType { get; set; }

        [Required(ErrorMessage = "StartDate là bắt buộc")]
        public DateOnly StartDate { get; set; }

        [Range(1, 100000, ErrorMessage = "TotalEggs phải lớn hơn 0")]
        public int? TotalEggs { get; set; }

        public string? Notes { get; set; }
    }
}
