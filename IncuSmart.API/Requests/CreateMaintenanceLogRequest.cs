namespace IncuSmart.API.Requests
{
    public class CreateMaintenanceLogRequest
    {
        [Required(ErrorMessage = CommonConst.DescriptionRequired)]
        [MaxLength(1000, ErrorMessage = CommonConst.DescriptionMaxLength)]
        public string Description { get; set; } = string.Empty;
    }
}
