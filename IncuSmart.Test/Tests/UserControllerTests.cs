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

        public UserControllerTests()
        {
            _controller = new UserController(_userUseCase.Object, _auditLogUseCase.Object);
            ControllerTestBase.SetupHttpContext(_controller, ControllerTestBase.AdminId, "ADMIN");

            // AuditLog luôn thành công
            _auditLogUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateAuditLogCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(Guid.NewGuid()));
        }

        // ─── Create ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Create_ValidRequest_Returns200()
        {
            var newId = Guid.NewGuid();
            _userUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<Guid?>(newId, "Tạo người dùng thành công"));

            var result = await _controller.Create(new CreateUserRequest
            {
                Username = "newstaff01",
                Password = "pass1234",
                FullName = "Nhân Viên Mới",
                Phone    = "0901234567",
                Role     = "SALES_STAFF"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Create_DuplicateUsername_Returns409()
        {
            _userUseCase.Setup(x => x.Create(It.IsAny<IncuSmart.Core.Commands.CreateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<Guid?>("Tên đăng nhập đã tồn tại"));

            var result = await _controller.Create(new CreateUserRequest
            {
                Username = "existuser",
                Password = "pass1234",
                FullName = "Nhân Viên",
                Phone    = "0901234567",
                Role     = "SALES_STAFF"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }

        // ─── List ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task List_NoFilter_Returns200WithPagedResult()
        {
            var paged = new PagedResult<User>
            {
                Items      = [new User { Id = Guid.NewGuid(), Username = "u1", FullName = "U1", PasswordHash = "", Phone = "", Role = IncuSmart.Core.Enums.UserRole.SALES_STAFF, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE }],
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

        [Fact]
        public async Task List_FilterByRole_Returns200()
        {
            var paged = new PagedResult<User> { Items = [], Page = 1, PageSize = 20, TotalItems = 0, TotalPages = 0 };
            _userUseCase.Setup(x => x.List("TECHNICIAN", null, 1, 20))
                .ReturnsAsync(ControllerTestBase.OkResult(paged));

            var result = await _controller.List("TECHNICIAN", null, 1, 20);

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetMe ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetMe_Authenticated_Returns200()
        {
            var user = new User { Id = ControllerTestBase.AdminId, Username = "admin", FullName = "Admin", PasswordHash = "", Phone = "", Role = IncuSmart.Core.Enums.UserRole.ADMIN, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _userUseCase.Setup(x => x.GetById(ControllerTestBase.AdminId))
                .ReturnsAsync(ControllerTestBase.OkResult<User?>(user));

            var result = await _controller.GetMe();

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── GetById ──────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetById_ExistingId_Returns200()
        {
            var userId = Guid.NewGuid();
            var user   = new User { Id = userId, Username = "staff1", FullName = "Nhân Viên 1", PasswordHash = "", Phone = "090", Role = IncuSmart.Core.Enums.UserRole.SALES_STAFF, Status = IncuSmart.Core.Enums.BaseStatus.ACTIVE };
            _userUseCase.Setup(x => x.GetById(userId))
                .ReturnsAsync(ControllerTestBase.OkResult<User?>(user));

            var result = await _controller.GetById(userId);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetById_NotExisting_Returns404()
        {
            var userId = Guid.NewGuid();
            _userUseCase.Setup(x => x.GetById(userId))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<User?>("Không tìm thấy người dùng"));

            var result = await _controller.GetById(userId);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Update ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Update_ValidRequest_Returns200()
        {
            var userId = Guid.NewGuid();
            _userUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.Update(userId, new UpdateUserRequest { FullName = "Tên Mới", Phone = "0901234567", Status = "ACTIVE" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Update_UserNotFound_Returns404()
        {
            var userId = Guid.NewGuid();
            _userUseCase.Setup(x => x.Update(It.IsAny<IncuSmart.Core.Commands.UpdateUserCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy người dùng"));

            var result = await _controller.Update(userId, new UpdateUserRequest { FullName = "Tên Mới", Phone = "090", Status = "ACTIVE" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── UpdateMe ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateMe_ValidRequest_Returns200()
        {
            _userUseCase.Setup(x => x.UpdateProfile(It.IsAny<IncuSmart.Core.Commands.UpdateProfileCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.UpdateMe(new UpdateProfileRequest { FullName = "Tên Mới", Phone = "0901234567" });

            result.Should().BeOfType<OkObjectResult>();
        }

        // ─── ChangePassword ───────────────────────────────────────────────────────────

        [Fact]
        public async Task ChangePassword_CorrectOldPassword_Returns200()
        {
            _userUseCase.Setup(x => x.ChangePassword(It.IsAny<IncuSmart.Core.Commands.ChangePasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "oldpass", NewPassword = "newpass" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ChangePassword_WrongOldPassword_Returns400()
        {
            _userUseCase.Setup(x => x.ChangePassword(It.IsAny<IncuSmart.Core.Commands.ChangePasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.BadRequestResult<bool>("Mật khẩu cũ không đúng"));

            var result = await _controller.ChangePassword(new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "newpass" });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // ─── ResetPassword ────────────────────────────────────────────────────────────

        [Fact]
        public async Task ResetPassword_ValidRequest_Returns200()
        {
            var userId = Guid.NewGuid();
            _userUseCase.Setup(x => x.ResetPassword(It.IsAny<IncuSmart.Core.Commands.ResetPasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult(true));

            var result = await _controller.ResetPassword(userId, new ResetPasswordRequest { NewPassword = "newpass123" });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ResetPassword_UserNotFound_Returns404()
        {
            var userId = Guid.NewGuid();
            _userUseCase.Setup(x => x.ResetPassword(It.IsAny<IncuSmart.Core.Commands.ResetPasswordCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<bool>("Không tìm thấy người dùng"));

            var result = await _controller.ResetPassword(userId, new ResetPasswordRequest { NewPassword = "newpass123" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
