namespace IncuSmart.Core.Usecases
{
    public class HatchingBatchUseCase : IHatchingBatchUseCase
    {
        private readonly IHatchingBatchRepository _batchRepo;
        private readonly IHatchingBatchConfigRepository _batchConfigRepo;
        private readonly IHatchingSeasonRepository _seasonRepo;
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<HatchingBatchUseCase> _logger;

        public HatchingBatchUseCase(
            IHatchingBatchRepository batchRepo,
            IHatchingBatchConfigRepository batchConfigRepo,
            IHatchingSeasonRepository seasonRepo,
            IIncubatorRepository incubatorRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<HatchingBatchUseCase> logger)
        {
            _batchRepo = batchRepo;
            _batchConfigRepo = batchConfigRepo;
            _seasonRepo = seasonRepo;
            _incubatorRepository = incubatorRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateHatchingBatchCommand command, Guid? currentUserId, string role)
        {
            var season = await _seasonRepo.FindById(command.SeasonId);
            if (season == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.HatchingSeasonNotFound, null);
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<Guid?>("404", CommonConst.IncubatorNotFound, null);
            }

            if (incubator.Status is not IncubatorStatus.ACTIVE and not IncubatorStatus.RESERVED)
            {
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.IncubatorNotAvailableForOperation, null);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<Guid?>("403", CommonConst.AccessDenied, null);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                var batch = new HatchingBatch
                {
                    Id = Guid.NewGuid(),
                    SeasonId = command.SeasonId,
                    BatchIndex = command.BatchIndex,
                    Name = command.Name,
                    DayStart = command.DayStart,
                    DayEnd = command.DayEnd,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                };
                await _batchRepo.Add(batch);

                foreach (var cfgCmd in command.Configs)
                {
                    await _batchConfigRepo.Add(new HatchingBatchConfig
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batch.Id,
                        ConfigId = cfgCmd.ConfigId,
                        TargetValue = cfgCmd.TargetValue,
                        MinValue = cfgCmd.MinValue,
                        MaxValue = cfgCmd.MaxValue,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                    });
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateHatchingBatchSuccessfully, batch.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating hatching batch");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<List<HatchingBatchDetailResponse>>> GetBySeasonId(Guid seasonId, Guid? currentUserId, string role)
        {
            var season = await _seasonRepo.FindById(seasonId);
            if (season == null)
            {
                return ResultModelUtils.FillResult<List<HatchingBatchDetailResponse>>("404", CommonConst.HatchingSeasonNotFound, new());
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<List<HatchingBatchDetailResponse>>("404", CommonConst.IncubatorNotFound, new());
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<List<HatchingBatchDetailResponse>>("403", CommonConst.AccessDenied, new());
            }

            var batches = await _batchRepo.FindBySeasonId(seasonId);
            var result = new List<HatchingBatchDetailResponse>();
            foreach (var batch in batches)
            {
                var configs = await _batchConfigRepo.FindByBatchId(batch.Id);
                result.Add(new HatchingBatchDetailResponse
                {
                    Batch = batch,
                    Configs = configs
                });
            }

            return ResultModelUtils.FillResult<List<HatchingBatchDetailResponse>>("200", CommonConst.Success, result);
        }

        public async Task<ResultModel<bool>> Update(UpdateHatchingBatchCommand command, Guid? currentUserId, string role)
        {
            var batch = await _batchRepo.FindById(command.Id);
            if (batch == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingBatchNotFound, false);
            }

            var season = await _seasonRepo.FindById(batch.SeasonId);
            if (season == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonNotFound, false);
            }

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
            {
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);
            }

            if (!await CanAccessIncubator(incubator, currentUserId, role))
            {
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                batch.Name = command.Name ?? batch.Name;
                batch.DayStart = command.DayStart ?? batch.DayStart;
                batch.DayEnd = command.DayEnd ?? batch.DayEnd;
                batch.ActualStartAt = command.ActualStartAt ?? batch.ActualStartAt;
                batch.ActualEndAt = command.ActualEndAt ?? batch.ActualEndAt;
                if (command.Status != null)
                {
                    batch.Status = Enum.Parse<BaseStatus>(command.Status);
                }
                batch.UpdatedAt = DateTime.UtcNow;
                batch.UpdatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;
                await _batchRepo.Update(batch);

                if (command.Configs != null)
                {
                    await _batchConfigRepo.SoftDeleteByBatchId(command.Id);
                    foreach (var cfgCmd in command.Configs)
                    {
                        await _batchConfigRepo.Add(new HatchingBatchConfig
                        {
                            Id = Guid.NewGuid(),
                            BatchId = command.Id,
                            ConfigId = cfgCmd.ConfigId,
                            TargetValue = cfgCmd.TargetValue,
                            MinValue = cfgCmd.MinValue,
                            MaxValue = cfgCmd.MaxValue,
                            Status = BaseStatus.ACTIVE,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = currentUserId?.ToString() ?? CommonConst.SystemActor,
                        });
                    }
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateHatchingBatchSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating hatching batch {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Delete(Guid id, Guid? currentUserId, string role)
        {
            var batch = await _batchRepo.FindById(id);
            if (batch == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingBatchNotFound, false);

            var season = await _seasonRepo.FindById(batch.SeasonId);
            if (season == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.HatchingSeasonNotFound, false);

            var incubator = await _incubatorRepository.FindById(season.IncubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);

            if (!await CanAccessIncubator(incubator, currentUserId, role))
                return ResultModelUtils.FillResult<bool>("403", CommonConst.AccessDenied, false);

            var deletedBy = currentUserId?.ToString() ?? CommonConst.SystemActor;
            await _unitOfWork.BeginAsync();
            try
            {
                await _batchConfigRepo.SoftDeleteByBatchId(id);
                await _batchRepo.SoftDelete(id, deletedBy);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.DeleteSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting hatching batch {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        private async Task<bool> CanAccessIncubator(Incubator incubator, Guid? currentUserId, string role)
        {
            if (role == UserRole.ADMIN.ToString() || role == UserRole.TECHNICIAN.ToString())
            {
                return true;
            }

            if (role != UserRole.CUSTOMER.ToString() || !currentUserId.HasValue)
            {
                return false;
            }

            var customer = await _customerRepository.FindByUserId(currentUserId.Value);
            return customer != null && incubator.CustomerId == customer.Id;
        }
    }
}
