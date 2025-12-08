using Xunit;
using Moq;
using FluentAssertions;
using ApexVision.Backend.Controllers;
using ApexVision.Backend.DTOs.Auth;
using ApexVision.Backend.Models;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ApexVision.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<JwtService> _mockJwtService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockUserManager = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null!, null!, null!, null!, null!, null!, null!, null!);
            
            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null!, null!, null!, null!);
            
            _mockJwtService = new Mock<JwtService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            
            _authController = new AuthController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockJwtService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Identifier = "driver@test.com",
                Password = "TestPassword123!"
            };

            var user = new User
            {
                Id = 1,
                Email = loginDto.Identifier,
                UserName = loginDto.Identifier,
                FullName = "Test Driver"
            };

            _mockUserManager
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockSignInManager
                .Setup(x => x.CheckPasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            _mockJwtService
                .Setup(x => x.GenerateToken(It.IsAny<User>()))
                .ReturnsAsync("test-jwt-token");

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Identifier = "driver@test.com",
                Password = "WrongPassword"
            };

            _mockUserManager
                .Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
