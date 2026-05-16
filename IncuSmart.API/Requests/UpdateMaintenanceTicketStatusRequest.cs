namespace IncuSmart.API.Requests
{
    public class UpdateMaintenanceTicketStatusRequest
    {
        [Required(ErrorMessage = CommonConst.StatusRequired)]
        [MaxLength(30, ErrorMessage = CommonConst.StatusMaxLength)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = CommonConst.ResolutionSummaryMaxLength)]
        public string? ResolutionSummary { get; set; }
    }
}
