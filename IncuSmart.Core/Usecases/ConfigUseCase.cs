using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Usecases
{
    public class ConfigUseCase : IConfigUseCase
    {
        private readonly IConfigRepository      _configRepository;
        private readonly IUnitOfWork            _unitOfWork;
        private readonly ILogger<ConfigUseCase> _logger;

        public ConfigUseCase(
            IConfigRepository configRepository,
            IUnitOfWork unitOfWork,
            ILogger<ConfigUseCase> logger)
        {
            _configRepository = configRepository;
            _unitOfWork       = unitOfWork;
            _logger           = logger;
        }

        // ─── CREATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<Guid?>> Create(CreateConfigCommand command)
        {
            // Kiểm tra Code đã tồn tại chưa
            var codeExists = await _configRepository.ExistsByCode(command.Code);
            if (codeExists)
                return ResultModelUtils.FillResult<Guid?>("409", $"Code '{command.Code}' đã tồn tại trong hệ thống", null);

            await _unitOfWork.BeginAsync();
            try
            {
                var config = new Config
                {
                    Id          = Guid.NewGuid(),
                    Code        = command.Code,
                    Name        = command.Name,
                    Type        = command.Type,
                    Unit        = command.Unit,
                    Description = command.Description,
                    Status      = BaseStatus.ACTIVE,
                    CreatedAt   = DateTime.UtcNow,
                    CreatedBy   = "SYSTEM",
                };
                await _configRepository.Add(config);
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<Guid?>("200", "Tạo cấu hình thiết bị thành công", config.Id);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error creating config");
                return ResultModelUtils.FillResult<Guid?>("500", ex.Message, null);
            }
        }

        // ─── GET BY ID ─────────────────────────────────────────────────────────────
        public async Task<ResultModel<Config?>> GetById(Guid id)
        {
            var config = await _configRepository.FindById(id);
            return config == null
                ? ResultModelUtils.FillResult<Config?>("404", "Không tìm thấy cấu hình thiết bị", null)
                : ResultModelUtils.FillResult<Config?>("200", "Success", config);
        }

        // ─── GET ALL ───────────────────────────────────────────────────────────────
        // Lọc theo: type (SENSOR | ACTUATOR), status (ACTIVE | INACTIVE)
        public async Task<ResultModel<List<Config>>> GetAll(string? type, string? status)
        {
            var list = await _configRepository.FindAll(type, status);
            return ResultModelUtils.FillResult<List<Config>>("200", "Success", list);
        }

        // ─── UPDATE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<bool>> Update(UpdateConfigCommand command)
        {
            var config = await _configRepository.FindById(command.Id);
            if (config == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy cấu hình thiết bị", false);

            await _unitOfWork.BeginAsync();
            try
            {
                // null = giữ nguyên giá trị cũ
                config.Name        = command.Name        ?? config.Name;
                config.Type        = command.Type        ?? config.Type;
                config.Unit        = command.Unit        ?? config.Unit;
                config.Description = command.Description ?? config.Description;
                config.UpdatedAt   = DateTime.UtcNow;
                config.UpdatedBy   = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Cập nhật cấu hình thiết bị thành công", true);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                _logger.LogError(ex, "Error updating config {Id}", command.Id);
                return ResultModelUtils.FillResult<bool>("500", ex.Message, false);
            }
        }

        // ─── DELETE ────────────────────────────────────────────────────────────────
        public async Task<ResultModel<bool>> Delete(Guid id)
        {
            var config = await _configRepository.FindById(id);
            if (config == null)
                return ResultModelUtils.FillResult<bool>("404", "Không tìm thấy cấu hình thiết bị", false);

            // Không xoá nếu đang được dùng trong incubator_model_configs
            var isUsed = await _configRepository.ExistsInModelConfig(id);
            if (isUsed)
                return ResultModelUtils.FillResult<bool>("400", "Cấu hình đang được sử dụng, không thể xoá", false);

            await _unitOfWork.BeginAsync();
            try
            {
                config.DeletedAt = DateTime.UtcNow;
                config.DeletedBy = "SYSTEM";
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedBy = "SYSTEM";
                await _unitOfWork.CommitAsync();
                return ResultModelUtils.FillResult<bool>("200", "Xoá cấu hình thiết bị thành công", true);
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
