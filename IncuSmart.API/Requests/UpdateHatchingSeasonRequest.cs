namespace IncuSmart.API.Requests
{
    public class UpdateHatchingSeasonRequest
    {
        public DateOnly? EndDate { get; set; }

        [Range(1, 100000, ErrorMessage = "TotalEggs phai lon hon 0")]
        public int? TotalEggs { get; set; }

        [Range(0, int.MaxValue)]
        public int? SuccessCount { get; set; }

        [Range(0, int.MaxValue)]
        public int? FailCount { get; set; }

        public string? Notes { get; set; }
    }
}
