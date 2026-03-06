using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Usecases
{
    public class IncubatorModelUseCase : IIncubatorModelUseCase
    {
        private readonly IIncubatorModelRepository _modelRepository;
        private readonly IIncubatorModelConfigRepository _modelConfigRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncubatorModelUseCase> _logger;

        public IncubatorModelUseCase(
            IIncubatorModelRepository modelRepository,
            IIncubatorModelConfigRepository modelConfigRepository,
            IUnitOfWork unitOfWork,
            ILogger<IncubatorModelUseCase> logger)
        {
            _modelRepository = modelRepository;
            _modelConfigRepository = modelConfigRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateIncubatorModelCommand command)
        {
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
                    Status = BaseStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "SYSTEM",
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
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "SYSTEM",
                    }).ToList();
                    await _modelConfigRepository.AddRange(configs);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Create incubator model successfully", modelId);
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
                ? ResultModelUtils.FillResult<IncubatorModel?>("404", "Incubator model not found", null)
                : ResultModelUtils.FillResult<IncubatorModel?>("200", "Success", model);
        }

        public async Task<ResultModel<List<IncubatorModel>>> GetAll()
        {
            var list = await _modelRepository.FindAll();
            return ResultModelUtils.FillResult<List<IncubatorModel>>("200", "Success", list);
        }

        public async Task<ResultModel<bool>> Update(UpdateIncubatorModelCommand command)
        {
            var model = await _modelRepository.FindById(command.Id);
            if (model == null)
                return ResultModelUtils.FillResult<bool>("404", "Incubator model not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                model.ModelCode = command.ModelCode;
                model.Name = command.Name;
                model.Description = command.Description;
                model.UpdatedAt = DateTime.UtcNow;
                model.UpdatedBy = "SYSTEM";

                await _modelConfigRepository.DeleteByModelId(command.Id);

                if (command.Configs.Any())
                {
                    var configs = command.Configs.Select(c => new IncubatorModelConfig
                    {
                        Id = Guid.NewGuid(),
                        ModelId = command.Id,
                        ConfigId = c.ConfigId,
                        Quantity = c.Quantity,
                        Required = c.Required,
                        Status = BaseStatus.ACTIVE,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "SYSTEM",
                    }).ToList();
                    await _modelConfigRepository.AddRange(configs);
                }

                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Update incubator model successfully", true);
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
                return ResultModelUtils.FillResult<bool>("404", "Incubator model not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                model.DeletedAt = DateTime.UtcNow;
                model.DeletedBy = "SYSTEM";
                model.UpdatedAt = DateTime.UtcNow;
                model.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Delete incubator model successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting incubator model {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }

}
