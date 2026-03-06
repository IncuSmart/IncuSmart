using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Usecases
{
    public class ConfigUseCase : IConfigUseCase
    {
        private readonly IConfigRepository _configRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConfigUseCase> _logger;

        public ConfigUseCase(IConfigRepository configRepository, IUnitOfWork unitOfWork, ILogger<ConfigUseCase> logger)
        {
            _configRepository = configRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateConfigCommand command)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var config = new Config
                {
                    Id = Guid.NewGuid(),
                    Code = command.Code,
                    Name = command.Name,
                    Type = command.Type,
                    Unit = command.Unit,
                    Description = command.Description,
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "SYSTEM",
                };
                await _configRepository.Add(config);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Create config successfully", config.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating config");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<Config?>> GetById(Guid id)
        {
            var config = await _configRepository.FindById(id);
            return config == null
                ? ResultModelUtils.FillResult<Config?>("404", "Config not found", null)
                : ResultModelUtils.FillResult<Config?>("200", "Success", config);
        }

        public async Task<ResultModel<List<Config>>> GetAll()
        {
            var list = await _configRepository.FindAll();
            return ResultModelUtils.FillResult<List<Config>>("200", "Success", list);
        }

        public async Task<ResultModel<bool>> Update(UpdateConfigCommand command)
        {
            var config = await _configRepository.FindById(command.Id);
            if (config == null)
                return ResultModelUtils.FillResult<bool>("404", "Config not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                config.Name = command.Name;
                config.Type = command.Type;
                config.Unit = command.Unit;
                config.Description = command.Description;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Update config successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating config {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Delete(Guid id)
        {
            var config = await _configRepository.FindById(id);
            if (config == null)
                return ResultModelUtils.FillResult<bool>("404", "Config not found", false);

            var isUsed = await _configRepository.ExistsInModelConfig(id);
            if (isUsed)
                return ResultModelUtils.FillResult<bool>("400", "Config is in use, cannot delete", false);

            await _unitOfWork.BeginAsync();
            try
            {
                config.DeletedAt = DateTime.UtcNow;
                config.DeletedBy = "SYSTEM";
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Delete config successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting config {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }

}
