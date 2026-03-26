using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Usecases
{
    public class IncubatorUseCase : IIncubatorUseCase
    {
        private readonly IIncubatorRepository _incubatorRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<IncubatorUseCase> _logger;

        public IncubatorUseCase(IIncubatorRepository incubatorRepository, IUnitOfWork unitOfWork, ILogger<IncubatorUseCase> logger)
        {
            _incubatorRepository = incubatorRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResultModel<Guid?>> Create(CreateIncubatorCommand command)
        {
            await _unitOfWork.BeginAsync();
            try
            {
                var incubator = new Incubator
                {
                    Id = Guid.NewGuid(),
                    ModelId = command.ModelId,
                    CustomerId = command.CustomerId,
                    ActivatedAt = command.ActivatedAt,
                    Status = IncubatorStatus.PENDING,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "SYSTEM",
                };
                await _incubatorRepository.Add(incubator);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Create successfully", incubator.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating incubator");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        public async Task<ResultModel<Incubator?>> GetById(Guid id)
        {
            var incubator = await _incubatorRepository.FindById(id);
            return incubator == null
                ? ResultModelUtils.FillResult<Incubator?>("404", "Incubator not found", null)
                : ResultModelUtils.FillResult<Incubator?>("200", "Success", incubator);
        }

        public async Task<ResultModel<List<Incubator>>> GetAll()
        {
            var list = await _incubatorRepository.FindAll();
            return ResultModelUtils.FillResult<List<Incubator>>("200", "Success", list);
        }

        public async Task<ResultModel<bool>> Update(UpdateIncubatorCommand command)
        {
            var incubator = await _incubatorRepository.FindById(command.Id);
            if (incubator == null)
                return ResultModelUtils.FillResult<bool>("404", "Incubator not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                incubator.ModelId = command.ModelId ?? incubator.ModelId;
                incubator.CustomerId = command.CustomerId ?? incubator.CustomerId;
                incubator.ActivatedAt = command.ActivatedAt ?? incubator.ActivatedAt;
                incubator.Status = command.Status ?? incubator.Status;
                incubator.UpdatedAt = DateTime.UtcNow;
                incubator.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Update successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating incubator {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        public async Task<ResultModel<bool>> Delete(Guid id)
        {
            var incubator = await _incubatorRepository.FindById(id);
            if (incubator == null)
                return ResultModelUtils.FillResult<bool>("404", "Incubator not found", false);

            await _unitOfWork.BeginAsync();
            try
            {
                incubator.DeletedAt = DateTime.UtcNow;
                incubator.DeletedBy = "SYSTEM";
                incubator.UpdatedAt = DateTime.UtcNow;
                incubator.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Delete successfully", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error deleting incubator {Id}", id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }
    }
}
