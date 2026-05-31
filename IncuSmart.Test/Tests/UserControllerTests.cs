using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Domain;
using IncuSmart.Core.Domains;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Core.Responses;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class UserControllerTests
    {
        private readonly Mock<IUserUseCase>     _userUseCase     = new();
        private readonly Mock<IAuditLogUseCase> _auditLogUseCase = new();
        private readonly UserController         _controller;

        private static readonly Guid SampleUserId = Guid.NewGuid();

        private static readonly User SampleUser = new()
        {
            Id           = SampleUserId,
            Username     = "staff01",
            FullName     = "Nguyễn Nhân Viên",
            PasswordHash = "",
            Phone        = "0901234567",
            Role         = IncuSmart.Core.Enums.UserRole.SALES_STAFF,
            Status       = IncuSmart.Core.Enums.BaseStatus.ACTIVE
        };

        public UserControllerTests()
        {
            _controller = new UserController(_userUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── F01: Create ──────────────────────────────────────────────────────────────

        [Fact] // F01-TC01: Tạo người dùng mới hợp lệ → 200
        public async Task Create_ValidRequest_Returns200()
        {
            _userUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(SampleUserId, "Tạo người dùng thành công"));

            var result = await _controller.Create(new CreateUserRequest
            {
                Username = "staff01",
                Password = "pass1234",
                FullName = "Nguyễn Nhân Viên",
                Phone    = "0901234567",
                Role     = "SALES_STAFF"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F01-TC02: Tên đăng nhập đã tồn tại → 409
        public async Task Create_DuplicateUsername_Returns409()
        {
            _userUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên đăng nhập đã tồn tại"));

            var result = await _controller.Create(new CreateUserRequest
            {
                Username = "staff01",
                Password = "pass1234",
                FullName = "Nguyễn Nhân Viên",
                Phone    = "0901234567",
                Role     = "SALES_STAFF"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── F02: List ────────────────────────────────────────────────────────────────

        [Fact] // F02-TC01: Lấy danh sách không lọc → 200
        public async Task List_NoFilter_Returns200WithPagedResult()
        {
            var paged = new PagedResult<User>
            {
                Items      = [SampleUser],
                Page       = 1,
                PageSize   = 20,
                TotalItems = 1,
                TotalPages = 1
            };
            _userUseCase.Setup(x => x.List(null, null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List(null, null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F02-TC02: Lọc theo role TECHNICIAN → 200
        public async Task List_FilterByRole_Returns200()
        {
            var paged = new PagedResult<User> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _userUseCase.Setup(x => x.List("TECHNICIAN", null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("TECHNICIAN", null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F03: GetMe ───────────────────────────────────────────────────────────────

        [Fact] // F03-TC01: Lấy thông tin cá nhân của admin đã xác thực → 200
        public async Task GetMe_Authenticated_Returns200()
        {
            var adminUser = new User
            {
                Id           = ControllerTestBase.AdminId,
                Username     = "admin",
                FullName     = "Quản Trị Viên",
                PasswordHash = "",
                Phone        = "0909090909",
                Role         = IncuSmart.Core.Enums.UserRole.ADMIN,
                Status       = IncuSmart.Core.Enums.BaseStatus.ACTIVE
            };
            _userUseCase.Setup(x => x.GetById(ControllerTestBase.AdminId))
                .ReturnsAsync(ControllerTestBase.OkResult<User?>(adminUser));

            var result = await _controller.GetMe();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F04: GetById ─────────────────────────────────────────────────────────────

        [Fact] // F04-TC01: Lấy chi tiết người dùng tồn tại → 200
        public async Task GetById_ExistingId_Returns200()
        {
            _userUseCase.Setup(x => x.GetById(SampleUserId))
                .ReturnsAsync(ControllerTestBase.OkResult<User?>(SampleUser));

            var result = await _controller.GetById(SampleUserId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F04-TC02: Người dùng không tồn tại → 404
        public async Task GetById_NotExisting_Returns404()
        {
            _userUseCase.Setup(x => x.GetById(It.IsAny<Guid>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<User?>("Không tìm thấy người dùng"));

            var result = await _controller.GetById(Guid.NewGuid());

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F05: Update ──────────────────────────────────────────────────────────────

        [Fact] // F05-TC01: Cập nhật thông tin hợp lệ → 200
        public async Task Update_ValidRequest_Returns200()
        {
            _userUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(SampleUserId, new UpdateUserRequest { FullName = "Tên Mới", Phone = "0901234567", Status = "ACTIVE" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F05-TC02: Người dùng không tồn tại → 404
        public async Task Update_UserNotFound_Returns404()
        {
            _userUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy người dùng"));

            var result = await _controller.Update(Guid.NewGuid(), new UpdateUserRequest { FullName = "Tên Mới", Phone = "090", Status = "ACTIVE" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── F06: UpdateMe ────────────────────────────────────────────────────────────

        [Fact] // F06-TC01: Cập nhật hồ sơ cá nhân hợp lệ → 200
        public async Task UpdateMe_ValidRequest_Returns200()
        {
            _userUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateMe(new UpdateProfileRequest { FullName = "Tên Mới", Phone = "0901234567" });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── F07: ChangePassword ──────────────────────────────────────────────────────

        [Fact] // F07-TC01: Đổi mật khẩu với mật khẩu cũ đúng → 200
        public async Task ChangePassword_CorrectOldPassword_Returns200()
        {
            _userUseCase.Setup(x => x.ChangePassword(It.IsAny<IncuSmart.Core.Commands.ChangePasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass123" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F07-TC02: Mật khẩu cũ không đúng → 400
        public async Task ChangePassword_WrongOldPassword_Returns400()
        {
            _userUseCase.Setup(x => x.ChangePassword(It.IsAny<IncuSmart.Core.Commands.ChangePasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Mật khẩu cũ không đúng"));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "wrongpass", NewPassword = "newpass123" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── F08: ResetPassword ───────────────────────────────────────────────────────

        [Fact] // F08-TC01: Reset mật khẩu hợp lệ → 200
        public async Task ResetPassword_ValidRequest_Returns200()
        {
            _userUseCase.Setup(x => x.ResetPassword(It.IsAny<IncuSmart.Core.Commands.ResetPasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ResetPassword(SampleUserId, new ResetPasswordRequest { NewPassword = "newpass123" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact] // F08-TC02: Người dùng không tồn tại → 404
        public async Task ResetPassword_UserNotFound_Returns404()
        {
            _userUseCase.Setup(x => x.ResetPassword(It.IsAny<IncuSmart.Core.Commands.ResetPasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy người dùng"));

            var result = await _controller.ResetPassword(Guid.NewGuid(), new ResetPasswordRequest { NewPassword = "newpass123" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
