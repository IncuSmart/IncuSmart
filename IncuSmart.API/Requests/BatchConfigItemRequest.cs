namespace IncuSmart.API.Requests
{
    public class BatchConfigItemRequest
    {
        [Required(ErrorMessage = "ConfigId là bắt buộc")]
        public Guid     ConfigId    { get; set; }
        public decimal? TargetValue { get; set; }
        public decimal? MinValue    { get; set; }
        public decimal? MaxValue    { get; set; }
    }
}
