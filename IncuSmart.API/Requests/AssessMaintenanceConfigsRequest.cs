namespace IncuSmart.API.Requests
{
    public class AssessMaintenanceConfigsRequest
    {
        [Required(ErrorMessage = CommonConst.AssessmentItemsRequired)]
        [MinLength(1, ErrorMessage = CommonConst.AssessmentItemsRequired)]
        public List<ConfigAssessmentItemRequest> Items { get; set; } = [];
    }

    public class ConfigAssessmentItemRequest
    {
        [Required(ErrorMessage = CommonConst.ConfigIdRequired)]
        public Guid ConfigId { get; set; }

        [Required(ErrorMessage = CommonConst.InvalidConfigCondition)]
        public string Condition { get; set; } = string.Empty;

        public long MarketPrice { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }
    }
}
