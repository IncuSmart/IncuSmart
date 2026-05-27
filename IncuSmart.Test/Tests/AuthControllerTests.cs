using FluentAssertions;
using IncuSmart.API.Controllers;
using IncuSmart.API.Requests;
using IncuSmart.Core.Ports.Inbound;
using IncuSmart.Test.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace IncuSmart.Test.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthUseCase>   _authUseCase = new();
        private readonly Mock<IConfiguration> _configuration = new();
        private readonly AuthController       _controller;

        public AuthControllerTests()
        {
            // JwtUtil dùng static JwtOptions — cần init trước khi gọi GenerateToken
            IncuSmart.Core.Utils.JwtOptions.Init(new IncuSmart.Core.Utils.JwtOptionsDto
            {
                Key           = "test-secret-key-at-least-32-chars!",
                Issuer        = "incusmart-test",
                Audience      = "incusmart-client",
                ExpireMinutes = 60
            });

            _configuration.Setup(c => c["AdminAuth:Username"]).Returns("admin");
            _configuration.Setup(c => c["AdminAuth:Password"]).Returns("admin123");
            _controller = new AuthController(_authUseCase.Object, _configuration.Object);
        }

        // ─── Login ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Login_ValidCredentials_Returns200()
        {
            _authUseCase.Setup(x => x.Login(It.IsAny<IncuSmart.Core.Commands.LoginCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<string?>("jwt.token.here", "Đăng nhập thành công"));

            var result = await _controller.Login(new LoginRequest { Username = "testuser", Password = "testpass" });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Login_WrongCredentials_Returns404()
        {
            _authUseCase.Setup(x => x.Login(It.IsAny<IncuSmart.Core.Commands.LoginCommand>()))
                .ReturnsAsync(ControllerTestBase.NotFoundResult<string?>("Sai tên đăng nhập hoặc mật khẩu"));

            var result = await _controller.Login(new LoginRequest { Username = "testuser", Password = "wrongpass" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── AdminLogin ───────────────────────────────────────────────────────────────

        [Fact]
        public void AdminLogin_ValidCredentials_Returns200WithToken()
        {
            var result = _controller.AdminLogin(new AdminLoginRequest { Username = "admin", Password = "admin123" });

            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.StatusCode.Should().Be(200);
        }

        [Fact]
        public void AdminLogin_WrongUsername_Returns404()
        {
            var result = _controller.AdminLogin(new AdminLoginRequest { Username = "wrong", Password = "admin123" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public void AdminLogin_WrongPassword_Returns404()
        {
            var result = _controller.AdminLogin(new AdminLoginRequest { Username = "admin", Password = "wrongpass" });

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ─── Register ─────────────────────────────────────────────────────────────────

        [Fact]
        public async Task Register_NewAccount_Returns200()
        {
            _authUseCase.Setup(x => x.Register(It.IsAny<IncuSmart.Core.Commands.RegisterCommand>()))
                .ReturnsAsync(ControllerTestBase.OkResult<string?>("jwt.token.here", "Đăng ký thành công"));

            var result = await _controller.Register(new RegisterRequest
            {
                Username = "newuser01",
                Password = "securepass",
                FullName = "Người Dùng Mới",
                Phone    = "0901234567"
            });

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Register_DuplicateUsername_Returns409()
        {
            _authUseCase.Setup(x => x.Register(It.IsAny<IncuSmart.Core.Commands.RegisterCommand>()))
                .ReturnsAsync(ControllerTestBase.ConflictResult<string?>("Tên đăng nhập đã tồn tại"));

            var result = await _controller.Register(new RegisterRequest
            {
                Username = "existuser",
                Password = "securepass",
                FullName = "Người Dùng Mới",
                Phone    = "0901234567"
            });

            result.Should().BeOfType<ConflictObjectResult>();
        }
    }
}
