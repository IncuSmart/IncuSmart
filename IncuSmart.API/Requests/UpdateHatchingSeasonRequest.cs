namespace IncuSmart.API.Requests
{
    public class UpdateHatchingSeasonRequest
    {
        [MaxLength(100, ErrorMessage = "Name không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        [MaxLength(50, ErrorMessage = "EggType không được vượt quá 50 ký tự")]
        public string? EggType { get; set; }

        public DateOnly? EndDate { get; set; }

        [Range(1, 100000, ErrorMessage = "TotalEggs phải lớn hơn 0")]
        public int? TotalEggs { get; set; }

        [Range(0, int.MaxValue)]
        public int? SuccessCount { get; set; }

        [Range(0, int.MaxValue)]
        public int? FailCount { get; set; }

        public string? Notes { get; set; }

        public string? Status { get; set; }
    }
}
