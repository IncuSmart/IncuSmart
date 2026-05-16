namespace IncuSmart.API.Requests
{
    public class UpdateHatchingSeasonStatusRequest
    {
        [Required(ErrorMessage = "Status la bat buoc")]
        public string Status { get; set; } = string.Empty;
    }
}
