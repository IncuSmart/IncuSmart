namespace IncuSmart.API.Requests
{
    public class CreateIncubatorRequest
    {
        [Required(ErrorMessage = CommonConst.ModelIdRequired)]
        public Guid ModelId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = CommonConst.QuantityMustBeGreaterThanZeroValidation)]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateConfigInstanceItemRequest
    {
        [Required(ErrorMessage = CommonConst.ConfigInstanceIdRequired)]
        public Guid ConfigInstanceId { get; set; }

        public decimal? TargetValue { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class UpdateConfigInstancesRequest
    {
        [Required(ErrorMessage = CommonConst.ItemsRequired)]
        public List<UpdateConfigInstanceItemRequest> Items { get; set; } = [];
    }
}
