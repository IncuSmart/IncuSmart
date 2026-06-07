namespace IncuSmart.API.Responses
{
    public class PredictHatchingSuccessResponse
    {
        public string EggType { get; set; } = string.Empty;

        public int TotalEggs { get; set; }

        public double? PredictedSuccessRate { get; set; }

        public double? PredictedSuccessPercent { get; set; }

        public double? Confidence { get; set; }

        public string PredictionMode { get; set; } = string.Empty;

        public int DbCompletedSeasons { get; set; }

        public int SyntheticReferences { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}
