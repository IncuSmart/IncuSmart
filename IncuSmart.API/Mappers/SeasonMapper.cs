namespace IncuSmart.API.Mappers
{
    public class SeasonMapper : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // Template
            config.NewConfig<BatchConfigItemRequest,              BatchConfigItemCommand>();
            config.NewConfig<TemplateBatchItemRequest,            TemplateBatchItemCommand>();
            config.NewConfig<CreateHatchingSeasonTemplateRequest, CreateHatchingSeasonTemplateCommand>();
            config.NewConfig<UpdateHatchingSeasonTemplateRequest, UpdateHatchingSeasonTemplateCommand>();
            config.NewConfig<TemplateBatchDetailCommand, TemplateBatchDetailResponse>();

            // Season
            config.NewConfig<CreateHatchingSeasonRequest, CreateHatchingSeasonCommand>();
            config.NewConfig<UpdateHatchingSeasonRequest, UpdateHatchingSeasonCommand>();
            config.NewConfig<HatchingSeasonTemplateDetailCommand, HatchingSeasonTemplateDetailResponse>();


            // Batch
            config.NewConfig<CreateHatchingBatchRequest, CreateHatchingBatchCommand>();
            config.NewConfig<UpdateHatchingBatchRequest, UpdateHatchingBatchCommand>();
            config.NewConfig<HatchingBatchDetailCommand, HatchingBatchDetailResponse>();

        }
    }
}
