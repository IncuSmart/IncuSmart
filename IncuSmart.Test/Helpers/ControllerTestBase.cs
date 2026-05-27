using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IncuSmart.Test.Helpers
{
    /// <summary>
    /// Base helper để setup HttpContext giả cho controller tests.
    /// </summary>
    public static class ControllerTestBase
    {
        public static readonly Guid AdminId    = Guid.Parse("00000000-0000-0000-0000-000000000001");
        public static readonly Guid CustomerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        public static readonly Guid TechId     = Guid.Parse("00000000-0000-0000-0000-000000000003");

        /// <summary>
        /// Gắn HttpContext với claims cho controller.
        /// </summary>
        public static void SetupHttpContext(ControllerBase controller, Guid userId, string role)
        {
            // Chỉ thêm NameIdentifier (không thêm Sub) để tránh SingleOrDefault throw
            // khi HttpContextUtils.GetId() dùng || trên cả 2 kiểu claim
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId.ToString()),
                new(ClaimTypes.Role, role),
                new(ClaimTypes.Name, "testuser")
            };
            var identity  = new ClaimsIdentity(claims, "test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        /// <summary>
        /// Tạo ResultModel success với data.
        /// </summary>
        public static IncuSmart.Core.ResultModel<T> OkResult<T>(T data, string message = "Thành công")
            => new() { StatusCode = "200", Message = message, Data = data };

        /// <summary>
        /// Tạo ResultModel not found.
        /// </summary>
        public static IncuSmart.Core.ResultModel<T> NotFoundResult<T>(string message = "Không tìm thấy")
            => new() { StatusCode = "404", Message = message, Data = default };

        /// <summary>
        /// Tạo ResultModel conflict.
        /// </summary>
        public static IncuSmart.Core.ResultModel<T> ConflictResult<T>(string message = "Đã tồn tại")
            => new() { StatusCode = "409", Message = message, Data = default };

        /// <summary>
        /// Tạo ResultModel bad request.
        /// </summary>
        public static IncuSmart.Core.ResultModel<T> BadRequestResult<T>(string message = "Dữ liệu không hợp lệ")
            => new() { StatusCode = "400", Message = message, Data = default };

        /// <summary>
        /// Tạo ResultModel internal server error.
        /// </summary>
        public static IncuSmart.Core.ResultModel<T> ErrorResult<T>(string message = "Lỗi hệ thống")
            => new() { StatusCode = "500", Message = message, Data = default };
    }
}
