namespace IncuSmart.API.Requests
{
    public class UpdateWarrantyRequest
    {
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate   { get; set; }
        public string?   Notes     { get; set; }
    }
}
