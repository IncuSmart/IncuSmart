namespace IncuSmart.API.Requests
{
    public class AssignMaintenanceTicketRequest
    {
        [Required(ErrorMessage = CommonConst.TechnicianIdRequired)]
        public Guid TechnicianId { get; set; }
    }
}
