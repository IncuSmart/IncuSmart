namespace IncuSmart.Core.Usecases
{
    public class HatchingBatchUseCase : IHatchingBatchUseCase
    {
        private readonly IHatchingBatchRepository        _batchRepo;
        private readonly IHatchingBatchConfigRepository  _batchConfigRepo;
        private readonly IHatchingSeasonRepository       _seasonRepo;
        private readonly IUnitOfWork                     _unitOfWork;
        private readonly ILogger<HatchingBatchUseCase>   _logger;

        public HatchingBatchUseCase(
            IHatchingBatchRepository batchRepo,
            IHatchingBatchConfigRepository batchConfigRepo,
            IHatchingSeasonRepository seasonRepo,
            IUnitOfWork unitOfWork,
            ILogger<HatchingBatchUseCase> logger)
        {
            _batchRepo       = batchRepo;
            _batchConfigRepo = batchConfigRepo;
            _seasonRepo      = seasonRepo;
            _unitOfWork      = unitOfWork;
            _logger          = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateHatchingBatchCommand command)
        {
            var season = await _seasonRepo.FindById(command.SeasonId);
            if (season == null)
                return ResultModelUtils.FillResult<Guid?>("404", "Không tìm thấy mùa ấp", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var batch = new HatchingBatch
                {
                    Id         = Guid.NewGuid(),
                    SeasonId   = command.SeasonId,
                    BatchIndex = command.BatchIndex,
                    Name       = command.Name,
                    DayStart   = command.DayStart,
                    DayEnd     = command.DayEnd,
                    Status     = BaseStatus.ACTIVE,
                    CreatedAt  = DateTime.UtcNow,
                    CreatedBy  = "SYSTEM",
                };
                await _batchRepo.Add(batch);

                foreach (var cfgCmd in command.Configs)
                {
                    await _batchConfigRepo.Add(new HatchingBatchConfig
                    {
                        Id          = Guid.NewGuid(),
                        BatchId     = batch.Id,
                        ConfigId    = cfgCmd.ConfigId,
                        TargetValue = cfgCmd.TargetValue,
                        MinValue    = cfgCmd.MinValue,
                        MaxValue    = cfgCmd.MaxValue,
                        Status      = BaseStatus.ACTIVE,
                        CreatedAt   = DateTime.UtcNow,
                        CreatedBy   = "SYSTEM",
                    });
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo giai đoạn ấp thành công", batch.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching batch");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY SEASON ID ──────────────────────────────────────────────────────
        public async Task<ResultModel<List<HatchingBatchDetailCommand>>> GetBySeasonId(Guid seasonId)
        {
            var season = await _seasonRepo.FindById(seasonId);
            if (season == null)
                return ResultModelUtils.FillResult<List<HatchingBatchDetailCommand>>("404", "Không tìm thấy mùa ấp", new());

            var batches = await _batchRepo.FindBySeasonId(seasonId);
            var result  = new List<HatchingBatchDetailCommand>();
            foreach (var batch in batches)
            {
                var configs = await _batchConfigRepo.FindByBatchId(batch.Id);
                result.Add(new HatchingBatchDetailCommand { Batch = batch, Configs = configs });
            }

            return ResultModelUtils.FillResult<List<HatchingBatchDetailCommand>>("200", "Success", result);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────────
        // Junction table configs: soft-delete cũ → insert mới nếu Configs != null
        public async Task<ResultModel<bool>> Update(UpdateHatchingBatchCommand command)
        {
            var batch = await _batchRepo.FindById(command.Id);
            if (batch == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy giai đoạn ấp", false);

            await _unitOfWork.BeginAsync();
            try
            {
                batch.Name          = command.Name          ?? batch.Name;
                batch.DayStart      = command.DayStart      ?? batch.DayStart;
                batch.DayEnd        = command.DayEnd        ?? batch.DayEnd;
                batch.ActualStartAt = command.ActualStartAt ?? batch.ActualStartAt;
                batch.ActualEndAt   = command.ActualEndAt   ?? batch.ActualEndAt;
                if (command.Status != null)
                    batch.Status = Enum.Parse<BaseStatus>(command.Status);
                batch.UpdatedAt = DateTime.UtcNow;
                batch.UpdatedBy = "SYSTEM";

                // Nếu gửi kèm Configs → soft-delete cũ, insert mới
                if (command.Configs != null)
                {
                    await _batchConfigRepo.SoftDeleteByBatchId(command.Id);
                    foreach (var cfgCmd in command.Configs)
                    {
                        await _batchConfigRepo.Add(new HatchingBatchConfig
                        {
                            Id          = Guid.NewGuid(),
                            BatchId     = command.Id,
                            ConfigId    = cfgCmd.ConfigId,
                            TargetValue = cfgCmd.TargetValue,
                            MinValue    = cfgCmd.MinValue,
                            MaxValue    = cfgCmd.MaxValue,
                            Status      = BaseStatus.ACTIVE,
                            CreatedAt   = DateTime.UtcNow,
                            CreatedBy   = "SYSTEM",
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Cập nhật giai đoạn ấp thành công", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating hatching batch {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
