namespace IncuSmart.API.Requests
{
    public class PredictHatchingSuccessRequest
    {
        [Required]
        public string EggType { get; set; } = string.Empty;

        [Range(1, 100000)]
        public int TotalEggs { get; set; }

        public Guid? IncubatorId { get; set; }

        public double? AmbientTemperature { get; set; }

        public double? AmbientHumidity { get; set; }
    }
}
