using IncuSmart.Core.Ports.Outbound;
using Microsoft.AspNetCore.Authorization;

namespace IncuSmart.API.Controllers
{
    [ApiController]
    public class UploadController(ICloudinaryService _cloudinaryService) : ApiControllerBase
    {
        [Authorize(Roles = "ADMIN,TECHNICIAN")]
        [HttpPost("api/upload/image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponse<string> { StatusCode = "400", Message = "Vui lòng chọn file ảnh.", Data = null });

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                return BadRequest(new BaseResponse<string> { StatusCode = "400", Message = "Chỉ chấp nhận file JPEG, PNG, WEBP hoặc GIF.", Data = null });

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _cloudinaryService.UploadImageAsync(stream, file.FileName);
                return Ok(new BaseResponse<string> { StatusCode = "200", Message = "Upload ảnh thành công.", Data = url });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<string> { StatusCode = "500", Message = ex.Message, Data = null });
            }
        }
    }
}
