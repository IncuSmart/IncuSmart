using IncuSmart.API.Services;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    [Authorize(Roles = "ADMIN")]
    public class AiController(IAiAdminClient _aiAdminClient) : ApiControllerBase
    {
        [HttpPost("training/sync")]
        public async Task<IActionResult> SyncTraining(CancellationToken ct)
        {
            try
            {
                var result = await _aiAdminClient.SyncTrainingAsync(ct);
                return Ok(new BaseResponse<AiTrainingSyncResult>
                {
                    StatusCode = "200",
                    Message = result?.Message ?? "Đồng bộ thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new BaseResponse<object>
                {
                    StatusCode = "503",
                    Message = $"Không thể kết nối AI service: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpPost("rag/upload")]
        public async Task<IActionResult> UploadRagDocument(IFormFile file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponse<object>
                {
                    StatusCode = "400",
                    Message = "File không được để trống",
                    Data = null
                });

            var allowed = new[] { ".txt", ".md", ".pdf" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return BadRequest(new BaseResponse<object>
                {
                    StatusCode = "400",
                    Message = $"Loại file không hỗ trợ: '{ext}'. Chỉ nhận .txt, .md, .pdf",
                    Data = null
                });

            try
            {
                var result = await _aiAdminClient.UploadRagDocumentAsync(file, ct);
                return Ok(new BaseResponse<AiRagUploadResult>
                {
                    StatusCode = "200",
                    Message = result?.Message ?? "Upload thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new BaseResponse<object>
                {
                    StatusCode = "503",
                    Message = $"Không thể kết nối AI service: {ex.Message}",
                    Data = null
                });
            }
        }

        [HttpGet("rag/status")]
        public async Task<IActionResult> GetRagStatus(CancellationToken ct)
        {
            try
            {
                var result = await _aiAdminClient.GetRagStatusAsync(ct);
                return Ok(new BaseResponse<AiRagStatusResult>
                {
                    StatusCode = "200",
                    Message = "Lấy trạng thái RAG thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new BaseResponse<object>
                {
                    StatusCode = "503",
                    Message = $"Không thể kết nối AI service: {ex.Message}",
                    Data = null
                });
            }
        }
    }
}
