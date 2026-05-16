namespace IncuSmart.Core.Usecases
{
    public class IncubatorModelUseCase : IIncubatorModelUseCase
    {
        private readonly IIncubatorModelRepository _modelRepository;
        private readonly IIncubatorModelConfigRepository _modelConfigRepository;
        private readonly IConfigRepository _configRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncubatorModelUseCase> _logger;

        public IncubatorModelUseCase(
            IIncubatorModelRepository modelRepository,
            IIncubatorModelConfigRepository modelConfigRepository,
            IConfigRepository configRepository,
            IUnitOfWork unitOfWork,
            ILogger<IncubatorModelUseCase> logger)
        {
            _modelRepository = modelRepository;
            _modelConfigRepository = modelConfigRepository;
            _configRepository = configRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateIncubatorModelCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.ModelCode) || string.IsNullOrWhiteSpace(command.Name))
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.ModelCodeAndNameRequired, null);

            if (command.UnitPrice <= 0)
                return ResultModelUtils.FillResult<Guid?>("400", CommonConst.UnitPriceMustBeGreaterThanZero, null);

            command.ModelCode = command.ModelCode.Trim();
            command.Name = command.Name.Trim();
            command.Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();

            var existingByCode = await _modelRepository.FindByModelCode(command.ModelCode);
            if (existingByCode != null)
                return ResultModelUtils.FillResult<Guid?>("409", CommonConst.ModelCodeAlreadyExists, null);

            var configValidation = await ValidateConfigs(command.Configs);
            if (configValidation != null)
                return configValidation;

            await _unitOfWork.BeginAsync();
            try
            {
                var modelId = Guid.NewGuid();
                var model = new IncubatorModel
                {
                    Id = modelId,
                    ModelCode = command.ModelCode,
                    Name = command.Name,
                    Description = command.Description,
                    UnitPrice = command.UnitPrice,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = CommonConst.SystemActor
                };
                await _modelRepository.Add(model);

                if (command.Configs.Any())
                {
                    var configs = command.Configs.Select(c => new IncubatorModelConfig
                    {
                        Id = Guid.NewGuid(),
                        ModelId = modelId,
                        ConfigId = c.ConfigId,
                        Quantity = c.Quantity,
                        Required = c.Required,
                        AbsoluteMin = c.AbsoluteMin,
                        AbsoluteMax = c.AbsoluteMax,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = CommonConst.SystemActor
                    }).ToList();

                    await _modelConfigRepository.AddRange(configs);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", CommonConst.CreateIncubatorModelSuccessfully, modelId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating incubator model");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<IncubatorModel?>> GetById(Guid id)
        {
            var model = await _modelRepository.FindById(id);
            return model == null
                ? ResultModelUtils.FillResult<IncubatorModel?>("404", CommonConst.IncubatorModelNotFound, null)
                : ResultModelUtils.FillResult<IncubatorModel?>("200", CommonConst.Success, model);
        }

        public async Task<ResultModel<PagedResult<IncubatorModel>>> List(string? status, string? search, int page, int pageSize)
        {
            var list = await _modelRepository.List(status, search);
            return ResultModelUtils.FillResult<PagedResult<IncubatorModel>>("200", CommonConst.Success, PagingUtils.ToPagedResult(list, page, pageSize));
        }

        public async Task<ResultModel<bool>> Update(UpdateIncubatorModelCommand command)
        {
            var model = await _modelRepository.FindById(command.Id);
            if (model == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorModelNotFound, false);

            var nextModelCode = command.ModelCode != null ? command.ModelCode.Trim() : model.ModelCode;
            var nextName = command.Name != null ? command.Name.Trim() : model.Name;
            var nextDescription = command.Description != null
                ? (string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim())
                : model.Description;
            var nextUnitPrice = command.UnitPrice ?? model.UnitPrice;

            if (nextUnitPrice <= 0)
                return ResultModelUtils.FillResult<bool>("400", CommonConst.UnitPriceMustBeGreaterThanZero, false);

            if (!string.Equals(nextModelCode, model.ModelCode, StringComparison.OrdinalIgnoreCase))
            {
                var existingByCode = await _modelRepository.FindByModelCode(nextModelCode);
                if (existingByCode != null && existingByCode.Id != model.Id)
                    return ResultModelUtils.FillResult<bool>("409", CommonConst.ModelCodeAlreadyExists, false);
            }

            if (command.Configs != null)
            {
                var configValidation = await ValidateConfigs(command.Configs);
                if (configValidation != null)
                    return ResultModelUtils.FillResult<bool>(configValidation.StatusCode, configValidation.Message, false);
            }

            await _unitOfWork.BeginAsync();
            try
            {
                model.ModelCode = nextModelCode;
                model.Name = nextName;
                model.Description = nextDescription;
                model.UnitPrice = nextUnitPrice;
                model.UpdatedAt = DateTime.UtcNow;
                model.UpdatedBy = CommonConst.SystemActor;
                await _modelRepository.Update(model);

                if (command.Configs != null)
                {
                    await _modelConfigRepository.SoftDeleteByModelId(command.Id);

                    var configs = command.Configs.Select(c => new IncubatorModelConfig
                    {
                        Id = Guid.NewGuid(),
                        ModelId = command.Id,
                        ConfigId = c.ConfigId,
                        Quantity = c.Quantity,
                        Required = c.Required,
                        AbsoluteMin = c.AbsoluteMin,
                        AbsoluteMax = c.AbsoluteMax,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = CommonConst.SystemActor
                    }).ToList();

                    await _modelConfigRepository.AddRange(configs);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.UpdateIncubatorModelSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating incubator model {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Delete(Guid id)
        {
            var model = await _modelRepository.FindById(id);
            if (model == null)
                return ResultModelUtils.FillResult<bool>("404", CommonConst.IncubatorModelNotFound, false);

            var hasIncubators = await _modelRepository.HasIncubators(id);
            if (hasIncubators)
                return ResultModelUtils.FillResult<bool>("400", CommonConst.CannotDeleteModelHasActiveIncubators, false);

            await _unitOfWork.BeginAsync();
            try
            {
                model.DeletedAt = DateTime.UtcNow;
                model.DeletedBy = CommonConst.SystemActor;
                model.UpdatedAt = DateTime.UtcNow;
                model.UpdatedBy = CommonConst.SystemActor;
                await _modelRepository.Update(model);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", CommonConst.DeleteIncubatorModelSuccessfully, true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting incubator model {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        private async Task<ResultModel<Guid?>?> ValidateConfigs(List<ModelConfigItemCommand> configs)
        {
            var configIds = new HashSet<Guid>();
            foreach (var configItem in configs)
            {
                if (!configIds.Add(configItem.ConfigId))
                    return ResultModelUtils.FillResult<Guid?>("400", string.Format(CommonConst.ConfigDuplicatedTemplate, configItem.ConfigId), null);

                var config = await _configRepository.FindById(configItem.ConfigId);
                if (config == null)
                    return ResultModelUtils.FillResult<Guid?>("400", string.Format(CommonConst.ConfigNotFoundTemplate, configItem.ConfigId), null);

                if (configItem.Quantity.HasValue && configItem.Quantity <= 0)
                    return ResultModelUtils.FillResult<Guid?>("400", string.Format(CommonConst.ConfigQuantityMustBeGreaterThanZeroTemplate, configItem.ConfigId), null);

                if (configItem.AbsoluteMin.HasValue && configItem.AbsoluteMax.HasValue && configItem.AbsoluteMin > configItem.AbsoluteMax)
                    return ResultModelUtils.FillResult<Guid?>("400", string.Format(CommonConst.AbsoluteMinCannotBeGreaterThanAbsoluteMaxTemplate, configItem.ConfigId), null);
            }

            return null;
        }
    }
}
