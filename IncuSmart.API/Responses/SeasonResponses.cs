using IncuSmart.Core.Domains;

namespace IncuSmart.API.Responses
{
    // Chi tiết Template kèm Batches + Configs
    public class HatchingSeasonTemplateDetailResponse
    {
        public HatchingSeasonTemplate?                Template { get; set; }
        public List<TemplateBatchDetailResponse>      Batches  { get; set; } = new();
    }

    public class TemplateBatchDetailResponse
    {
        public HatchingSeasonTemplateBatch?           Batch   { get; set; }
        public List<HatchingSeasonTemplateBatchConfig> Configs { get; set; } = new();
    }

    // Giai đoạn ấp kèm Configs
    public class HatchingBatchDetailResponse
    {
        public HatchingBatch?           Batch   { get; set; }
        public List<HatchingBatchConfig> Configs { get; set; } = new();
    }
}
