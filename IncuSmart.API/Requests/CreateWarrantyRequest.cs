namespace IncuSmart.API.Requests
{
    public class CreateWarrantyRequest
    {
        [Required(ErrorMessage = "IncubatorId là bắt buộc")]
        public Guid IncubatorId { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate   { get; set; }
        public string?   Notes     { get; set; }
    }
}
