namespace IncuSmart.Core.Usecases
{
    public class IncubatorUseCase : IIncubatorUseCase
    {
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly IIncubatorModelRepository _modelRepository;
        private readonly IIncubatorModelConfigRepository _modelConfigRepository;
        private readonly IIncubatorConfigInstanceRepository _configInstanceRepository;
        private readonly IHatchingSeasonRepository _hatchingSeasonRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncubatorUseCase> _logger;

        public IncubatorUseCase(
            IIncubatorRepository incubatorRepository,
            IIncubatorModelRepository modelRepository,
            IIncubatorModelConfigRepository modelConfigRepository,
            IIncubatorConfigInstanceRepository configInstanceRepository,
            IHatchingSeasonRepository hatchingSeasonRepository,
            ICustomerRepository customerRepository,
            IUnitOfWork unitOfWork,
            ILogger<IncubatorUseCase> logger)
        {
            _incubatorRepository = incubatorRepository;
            _modelRepository = modelRepository;
            _modelConfigRepository = modelConfigRepository;
            _configInstanceRepository = configInstanceRepository;
            _hatchingSeasonRepository = hatchingSeasonRepository;
            _customerRepository = customerRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<List<Guid>>> Create(CreateIncubatorCommand command)
        {
            var model = await _modelRepository.FindById(command.ModelId);
            if (model == null || model.Status != BaseStatus.ACTIVE)
                return ResultModelUtils.FillResult<List<Guid>>("400", CommonConst.IncubatorModelNotFoundOrInactive, null);

            if (command.Quantity <= 0)
                return ResultModelUtils.FillResult<List<Guid>>("400", CommonConst.QuantityMustBeGreaterThanZero, null);

            var modelConfigs = await _modelConfigRepository.FindByModelId(command.ModelId);

            await _unitOfWork.BeginAsync();
            try
            {
                var incubatorIds = new List<Guid>();
                var configInstances = new List<IncubatorConfigInstance>();
                var now = DateTime.UtcNow;

                for (var index = 0; index < command.Quantity; index++)
                {
                    var incubatorId = Guid.NewGuid();
                    incubatorIds.Add(incubatorId);

                    string serialNumber;
                    do { serialNumber = $"{model.ModelCode}-{CodeGenUtils.GenerateSecureCode(6)}"; }
                    while (await _incubatorRepository.ExistsBySerialNumber(serialNumber));

                    var incubator = new Incubator
                    {
                        Id = incubatorId,
                        ModelId = command.ModelId,
                        SerialNumber = serialNumber,
                        CustomerId = null,
                        ActivatedAt = null,
                        Status = IncubatorStatus.AVAILABLE,
                        CreatedAt = now,
                        CreatedBy = CommonConst.SystemActor
                    };
                    await _incubatorRepository.Add(incubator);

                    configInstances.AddRange(modelConfigs
                        .SelectMany(mc => Enumerable.Range(0, mc.Quantity ?? 1)
                            .Select(i => new IncubatorConfigInstance
                            {
                                Id = Guid.NewGuid(),
                                IncubatorId = incubatorId,
                                ConfigId = mc.ConfigId,
                                InstanceIndex = i,
                                Status = BaseStatus.ACTIVE,
                                CreatedAt = now,
                                CreatedBy = CommonConst.SystemActor
                            })));
                }

                if (configInstances.Any())
                {
                    await _configInstanceRepository.AddRange(configInstances);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult("200", CommonConst.CreateIncubatorSuccessfully, incubatorIds);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating incubators");
                return ResultModelUtils.FillResult<List<Guid>>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<IncubatorResponse?>> GetById(Guid id, Guid requesterId, string requesterRole)
        {
            var incubator = await _incubatorRepository.FindDetailById(id);
            if (incubator == null)
                return ResultModelUtils.FillResult<IncubatorResponse?>("404", CommonConst.IncubatorNotFound, null);

            if (requesterRole == UserRole.CUSTOMER.ToString())
            {
                var customer = await _customerRepository.FindByUserId(requesterId);
                if (customer == null || incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<IncubatorResponse?>("403", CommonConst.AccessDenied, null);
            }

            return ResultModelUtils.FillResult<IncubatorResponse?>("200", CommonConst.Success, incubator);
        }

        public async Task<ResultModel<PagedResult<IncubatorResponse>>> List(Guid requesterId, string requesterRole, string? status, Guid? modelId, int page, int pageSize)
        {
            Guid? customerId = null;
            if (requesterRole == UserRole.CUSTOMER.ToString())
            {
                var customer = await _customerRepository.FindByUserId(requesterId);
                if (customer == null)
                    return ResultModelUtils.FillResult<PagedResult<IncubatorResponse>>("404", CommonConst.CustomerNotFound, null);
                customerId = customer.Id;
            }

            var list = await _incubatorRepository.List(customerId, status, modelId);
            return ResultModelUtils.FillResult<PagedResult<IncubatorResponse>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> UpdateConfigInstances(UpdateConfigInstancesCommand command)
        {
            var incubator = await _incubatorRepository.FindById(command.IncubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);

            var modelConfigs = await _modelConfigRepository.FindByModelId(incubator.ModelId);

            var instances = new List<IncubatorConfigInstance>();
            foreach (var item in command.Items)
            {
                var instance = await _configInstanceRepository.FindById(item.ConfigInstanceId);
                if (instance == null || instance.IncubatorId != command.IncubatorId)
                    return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.ConfigInstanceDoesNotBelongToIncubatorTemplate, item.ConfigInstanceId), false);

                var modelConfig = modelConfigs.FirstOrDefault(mc => mc.ConfigId == instance.ConfigId);
                if (modelConfig != null)
                {
                    var target = item.TargetValue ?? instance.TargetValue;
                    var min = item.MinValue ?? instance.MinValue;
                    var max = item.MaxValue ?? instance.MaxValue;

                    if (min.HasValue && max.HasValue && min > max)
                        return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.MinValueCannotBeGreaterThanMaxValueTemplate, item.ConfigInstanceId), false);

                    if (target.HasValue && min.HasValue && target < min)
                        return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.TargetValueCannotBeLowerThanMinValueTemplate, item.ConfigInstanceId), false);

                    if (target.HasValue && max.HasValue && target > max)
                        return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.TargetValueCannotBeGreaterThanMaxValueTemplate, item.ConfigInstanceId), false);

                    if (modelConfig.AbsoluteMin.HasValue && ((target.HasValue && target < modelConfig.AbsoluteMin) || (min.HasValue && min < modelConfig.AbsoluteMin)))
                        return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.ValueBelowAbsoluteMinimumTemplate, modelConfig.AbsoluteMin, item.ConfigInstanceId), false);

                    if (modelConfig.AbsoluteMax.HasValue && ((target.HasValue && target > modelConfig.AbsoluteMax) || (max.HasValue && max > modelConfig.AbsoluteMax)))
                        return ResultModelUtils.FillResult<bool>("400", string.Format(CommonConst.ValueExceedsAbsoluteMaximumTemplate, modelConfig.AbsoluteMax, item.ConfigInstanceId), false);
                }

                instances.Add(instance);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                foreach (var item in command.Items)
                {
                    var instance = instances.First(x => x.Id == item.ConfigInstanceId);

                    instance.TargetValue = item.TargetValue ?? instance.TargetValue;
                    instance.MinValue = item.MinValue ?? instance.MinValue;
                    instance.MaxValue = item.MaxValue ?? instance.MaxValue;
                    instance.UpdatedAt = DateTime.UtcNow;
                    instance.UpdatedBy = CommonConst.SystemActor;

                    await _configInstanceRepository.Update(instance);
                }

                await _unitOfWork.CommitAsync();

                // TODO: Publish MQTT topic incubator/{incubatorId}/config/set
                _logger.LogInformation("Config instances updated for incubator {IncubatorId}. MQTT publish pending.", command.IncubatorId);

                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateConfigInstancesSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating config instances for incubator {IncubatorId}", command.IncubatorId);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> UpdateStatus(Guid id, string status, string updatedBy)
        {
            if (!Enum.TryParse<IncubatorStatus>(status, true, out var newStatus))
                return ResultModelUtils.FillResult<bool>("400", CommonConst.InvalidIncubatorStatus, false);

            var incubator = await _incubatorRepository.FindById(id);
            if (incubator == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorNotFound, false);

            await _unitOfWork.BeginAsync();
            try
            {
                incubator.Status = newStatus;
                incubator.UpdatedAt = DateTime.UtcNow;
                incubator.UpdatedBy = updatedBy;
                await _incubatorRepository.Update(incubator);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateIncubatorStatusSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating incubator status {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<List<HatchingSeason>>> GetHatchingSeasons(Guid incubatorId, Guid requesterId, string requesterRole, string? status, string? eggType)
        {
            var incubator = await _incubatorRepository.FindById(incubatorId);
            if (incubator == null)
                return ResultModelUtils.FillResult<List<HatchingSeason>>("404", CommonConst.IncubatorNotFound, null);

            if (requesterRole == UserRole.CUSTOMER.ToString())
            {
                var customer = await _customerRepository.FindByUserId(requesterId);
                if (customer == null || incubator.CustomerId != customer.Id)
                    return ResultModelUtils.FillResult<List<HatchingSeason>>("403", CommonConst.AccessDenied, null);
            }

            var seasons = await _hatchingSeasonRepository.FindByIncubatorId(incubatorId, status, eggType);
            return ResultModelUtils.FillResult<List<HatchingSeason>>("200", CommonConst.Success, seasons);
        }
    }
}
