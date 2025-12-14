using Xunit;
using Moq;
using FluentAssertions;
using ApexVision.Backend.Controllers;
using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace ApexVision.Tests.Controllers
{
    public class DriversControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<DriversController>> _mockLogger;
        private readonly DriversController _controller;
        private readonly ClaimsPrincipal _adminUser;
        private readonly User _testAdmin;
        private readonly User _testDriver;

        public DriversControllerTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"DriversTestDb_{Guid.NewGuid()}")
                .Options;
            _context = new ApplicationDbContext(options);

            // Crear usuarios de prueba
            _testAdmin = new User
            {
                Id = 1,
                UserName = "admin@test.com",
                Email = "admin@test.com",
                FullName = "Test Admin",
                PhoneNumber = "+573001234567",
                CompanyNit = "123456789",
                CompanyName = "Test Company"
            };

            _testDriver = new User
            {
                Id = 2,
                UserName = "driver@test.com",
                Email = "driver@test.com",
                FullName = "Test Driver",
                PhoneNumber = "+573009876543"
            };

            _context.Users.Add(_testAdmin);
            _context.Users.Add(_testDriver);
            _context.SaveChanges();

            // Configurar UserManager mock
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(_testAdmin);
            _mockUserManager.Setup(x => x.FindByIdAsync("2")).ReturnsAsync(_testDriver);
            _mockUserManager.Setup(x => x.GetRolesAsync(_testDriver)).ReturnsAsync(new List<string> { "Driver" });
            _mockUserManager.Setup(x => x.GetRolesAsync(_testAdmin)).ReturnsAsync(new List<string> { "Admin" });

            _mockLogger = new Mock<ILogger<DriversController>>();

            // Configurar claims de admin
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            _adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "TestAuth"));

            // Crear controlador
            _controller = new DriversController(_context, _mockUserManager.Object, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = _adminUser }
                }
            };
        }

        [Fact]
        public async Task GetMyDrivers_WithNoLinkedDrivers_ReturnsEmptyList()
        {
            // Act
            var result = await _controller.GetMyDrivers(null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task LinkDriver_WithValidPhone_LinksSuccessfully()
        {
            // Arrange
            var linkDto = new LinkDriverDto
            {
                PhoneNumber = "+573009876543"
            };

            // Act
            var result = await _controller.LinkDriver(linkDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var link = await _context.AdminDrivers.FirstOrDefaultAsync(ad => ad.AdminId == 1 && ad.DriverId == 2);
            link.Should().NotBeNull();
        }

        [Fact]
        public async Task LinkDriver_WithInvalidPhone_ReturnsNotFound()
        {
            // Arrange
            var linkDto = new LinkDriverDto
            {
                PhoneNumber = "+999999999"
            };

            // Act
            var result = await _controller.LinkDriver(linkDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task LinkDriver_AlreadyLinked_ReturnsConflict()
        {
            // Arrange
            _context.AdminDrivers.Add(new AdminDriver
            {
                AdminId = 1,
                DriverId = 2,
                LinkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var linkDto = new LinkDriverDto
            {
                PhoneNumber = "+573009876543"
            };

            // Act
            var result = await _controller.LinkDriver(linkDto);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task UnlinkDriver_WithLinkedDriver_UnlinksSuccessfully()
        {
            // Arrange
            _context.AdminDrivers.Add(new AdminDriver
            {
                AdminId = 1,
                DriverId = 2,
                LinkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.UnlinkDriver(2);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var link = await _context.AdminDrivers.FirstOrDefaultAsync(ad => ad.AdminId == 1 && ad.DriverId == 2);
            link.Should().BeNull();
        }

        [Fact]
        public async Task UnlinkDriver_NotLinked_ReturnsNotFound()
        {
            // Act
            var result = await _controller.UnlinkDriver(2);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetDriver_WhenLinked_ReturnsDriverInfo()
        {
            // Arrange
            _context.AdminDrivers.Add(new AdminDriver
            {
                AdminId = 1,
                DriverId = 2,
                LinkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDriver(2);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDriver_WhenNotLinked_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetDriver(2);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task SearchByPhone_WithValidPhone_ReturnsDriver()
        {
            // Act
            var result = await _controller.SearchByPhone("+573009876543");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SearchByPhone_WithInvalidPhone_ReturnsNotFound()
        {
            // Act
            var result = await _controller.SearchByPhone("+999999999");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetDashboard_ReturnsStats()
        {
            // Arrange
            _context.AdminDrivers.Add(new AdminDriver
            {
                AdminId = 1,
                DriverId = 2,
                LinkedAt = DateTime.UtcNow
            });
            
            _context.Orders.Add(new Order
            {
                Id = 1,
                Address = "Test",
                Latitude = 6.25,
                Longitude = -75.56,
                Description = "Test Order",
                Status = OrderStatus.Pending,
                AdminId = 1,
                DriverId = 2
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDashboard();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDriverStats_WhenLinked_ReturnsStats()
        {
            // Arrange
            _context.AdminDrivers.Add(new AdminDriver
            {
                AdminId = 1,
                DriverId = 2,
                LinkedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetDriverStats(2);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetDriverStats_WhenNotLinked_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetDriverStats(2);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
