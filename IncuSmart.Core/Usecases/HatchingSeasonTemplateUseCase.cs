namespace IncuSmart.Core.Usecases
{
    public class HatchingSeasonTemplateUseCase : IHatchingSeasonTemplateUseCase
    {
        private readonly IHatchingSeasonTemplateRepository             _templateRepo;
        private readonly IHatchingSeasonTemplateBatchRepository        _batchRepo;
        private readonly IHatchingSeasonTemplateBatchConfigRepository  _batchConfigRepo;
        private readonly IUnitOfWork                                    _unitOfWork;
        private readonly ILogger<HatchingSeasonTemplateUseCase>         _logger;

        public HatchingSeasonTemplateUseCase(
            IHatchingSeasonTemplateRepository templateRepo,
            IHatchingSeasonTemplateBatchRepository batchRepo,
            IHatchingSeasonTemplateBatchConfigRepository batchConfigRepo,
            IUnitOfWork unitOfWork,
            ILogger<HatchingSeasonTemplateUseCase> logger)
        {
            _templateRepo    = templateRepo;
            _batchRepo       = batchRepo;
            _batchConfigRepo = batchConfigRepo;
            _unitOfWork      = unitOfWork;
            _logger          = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateHatchingSeasonTemplateCommand command)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var template = new HatchingSeasonTemplate
                {
                    Id            = Guid.NewGuid(),
                    CustomerId    = command.CustomerId,
                    Name          = command.Name,
                    Description   = command.Description,
                    TotalDays     = command.TotalDays,
                    EggType       = command.EggType,
                    IsActive      = true,
                    CreatedByType = command.CreatedByType,
                    Status        = BaseStatus.ACTIVE,
                    CreatedAt     = DateTime.UtcNow,
                    CreatedBy     = "SYSTEM",
                };
                await _templateRepo.Add(template);

                // Insert nested batches + configs
                foreach (var batchCmd in command.Batches)
                {
                    var batch = new HatchingSeasonTemplateBatch
                    {
                        Id         = Guid.NewGuid(),
                        TemplateId = template.Id,
                        BatchIndex = batchCmd.BatchIndex,
                        Name       = batchCmd.Name,
                        DayStart   = batchCmd.DayStart,
                        DayEnd     = batchCmd.DayEnd,
                        Notes      = batchCmd.Notes,
                        Status     = BaseStatus.ACTIVE,
                        CreatedAt  = DateTime.UtcNow,
                        CreatedBy  = "SYSTEM",
                    };
                    await _batchRepo.Add(batch);

                    foreach (var cfgCmd in batchCmd.Configs)
                    {
                        await _batchConfigRepo.Add(new HatchingSeasonTemplateBatchConfig
                        {
                            Id              = Guid.NewGuid(),
                            TemplateBatchId = batch.Id,
                            ConfigId        = cfgCmd.ConfigId,
                            TargetValue     = cfgCmd.TargetValue,
                            MinValue        = cfgCmd.MinValue,
                            MaxValue        = cfgCmd.MaxValue,
                            Status          = BaseStatus.ACTIVE,
                            CreatedAt       = DateTime.UtcNow,
                            CreatedBy       = "SYSTEM",
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo mẫu mùa ấp thành công", template.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching season template");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY ID ─────────────────────────────────────────────────────────────
        public async Task<ResultModel<HatchingSeasonTemplateDetailCommand?>> GetById(Guid id)
        {
            var template = await _templateRepo.FindById(id);
            if (template == null)
                return ResultModelUtils.FillResult<HatchingSeasonTemplateDetailCommand?>("404", "Không tìm thấy mẫu mùa ấp", null);

            var batches = await _batchRepo.FindByTemplateId(id);
            var batchDetails = new List<TemplateBatchDetailCommand>();
            foreach (var batch in batches)
            {
                var configs = await _batchConfigRepo.FindByTemplateBatchId(batch.Id);
                batchDetails.Add(new TemplateBatchDetailCommand { Batch = batch, Configs = configs });
            }

            var response = new HatchingSeasonTemplateDetailCommand { Template = template, Batches = batchDetails };
            return ResultModelUtils.FillResult<HatchingSeasonTemplateDetailCommand?>("200", "Success", response);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────────
        // Lọc theo customerId (CUSTOMER xem template cá nhân + public),
        // createdByType (CUSTOMER | TECHNICIAN)
        public async Task<ResultModel<List<HatchingSeasonTemplate>>> GetAll(Guid? customerId, string? createdByType)
        {
            var list = await _templateRepo.FindAll(customerId, createdByType);
            return ResultModelUtils.FillResult<List<HatchingSeasonTemplate>>("200", "Success", list);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────────
        // Junction table: soft-delete batches cũ → insert mới nếu Batches != null
        public async Task<ResultModel<bool>> Update(UpdateHatchingSeasonTemplateCommand command)
        {
            var template = await _templateRepo.FindById(command.Id);
            if (template == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy mẫu mùa ấp", false);

            await _unitOfWork.BeginAsync();
            try
            {
                template.Name        = command.Name        ?? template.Name;
                template.Description = command.Description ?? template.Description;
                template.TotalDays   = command.TotalDays   ?? template.TotalDays;
                template.EggType     = command.EggType     ?? template.EggType;
                template.IsActive    = command.IsActive    ?? template.IsActive;
                template.UpdatedAt   = DateTime.UtcNow;
                template.UpdatedBy   = "SYSTEM";

                // Nếu gửi kèm Batches → soft-delete cũ, insert mới
                if (command.Batches != null)
                {
                    var oldBatches = await _batchRepo.FindByTemplateId(command.Id);
                    foreach (var old in oldBatches)
                    {
                        await _batchConfigRepo.SoftDeleteByTemplateBatchId(old.Id);
                    }
                    await _batchRepo.SoftDeleteByTemplateId(command.Id);

                    foreach (var batchCmd in command.Batches)
                    {
                        var batch = new HatchingSeasonTemplateBatch
                        {
                            Id         = Guid.NewGuid(),
                            TemplateId = command.Id,
                            BatchIndex = batchCmd.BatchIndex,
                            Name       = batchCmd.Name,
                            DayStart   = batchCmd.DayStart,
                            DayEnd     = batchCmd.DayEnd,
                            Notes      = batchCmd.Notes,
                            Status     = BaseStatus.ACTIVE,
                            CreatedAt  = DateTime.UtcNow,
                            CreatedBy  = "SYSTEM",
                        };
                        await _batchRepo.Add(batch);

                        foreach (var cfgCmd in batchCmd.Configs)
                        {
                            await _batchConfigRepo.Add(new HatchingSeasonTemplateBatchConfig
                            {
                                Id              = Guid.NewGuid(),
                                TemplateBatchId = batch.Id,
                                ConfigId        = cfgCmd.ConfigId,
                                TargetValue     = cfgCmd.TargetValue,
                                MinValue        = cfgCmd.MinValue,
                                MaxValue        = cfgCmd.MaxValue,
                                Status          = BaseStatus.ACTIVE,
                                CreatedAt       = DateTime.UtcNow,
                                CreatedBy       = "SYSTEM",
                            });
                        }
                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Cập nhật mẫu mùa ấp thành công", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating template {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
