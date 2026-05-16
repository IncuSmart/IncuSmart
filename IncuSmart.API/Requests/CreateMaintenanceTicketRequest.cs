namespace IncuSmart.API.Requests
{
    public class CreateMaintenanceTicketRequest
    {
        [Required(ErrorMessage = CommonConst.IncubatorIdRequired)]
        public Guid IncubatorId { get; set; }

        public Guid? TechnicianId { get; set; }

        [Required(ErrorMessage = CommonConst.IssueDescriptionRequired)]
        [MaxLength(1000, ErrorMessage = CommonConst.IssueDescriptionMaxLength)]
        public string IssueDescription { get; set; } = string.Empty;
    }
}
