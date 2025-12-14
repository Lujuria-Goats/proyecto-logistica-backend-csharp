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
using System.Text.Json;

namespace ApexVision.Tests.Controllers
{
    public class RoutesControllerTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ILogger<RoutesController>> _mockLogger;
        private readonly RoutesController _controller;
        private readonly ClaimsPrincipal _adminUser;
        private readonly ClaimsPrincipal _driverUser;
        private readonly User _testAdmin;
        private readonly User _testDriver;

        public RoutesControllerTests()
        {
            // Configurar base de datos en memoria
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"RoutesTestDb_{Guid.NewGuid()}")
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

            // Crear órdenes de prueba
            var orders = new List<Order>
            {
                new Order { Id = 1, Address = "Calle 1", Latitude = 6.25, Longitude = -75.56, Description = "Orden 1", Status = OrderStatus.Pending, AdminId = 1, DriverId = 2 },
                new Order { Id = 2, Address = "Calle 2", Latitude = 6.26, Longitude = -75.57, Description = "Orden 2", Status = OrderStatus.Pending, AdminId = 1, DriverId = 2 },
                new Order { Id = 3, Address = "Calle 3", Latitude = 6.27, Longitude = -75.58, Description = "Orden 3", Status = OrderStatus.Pending, AdminId = 1, DriverId = 2 }
            };
            _context.Orders.AddRange(orders);
            _context.SaveChanges();

            // Configurar UserManager mock
            var userStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _mockUserManager.Setup(x => x.FindByIdAsync("1")).ReturnsAsync(_testAdmin);
            _mockUserManager.Setup(x => x.FindByIdAsync("2")).ReturnsAsync(_testDriver);
            _mockUserManager.Setup(x => x.Users).Returns(_context.Users);

            _mockLogger = new Mock<ILogger<RoutesController>>();

            // Configurar claims de usuarios
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            _adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "TestAuth"));

            var driverClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Email, "driver@test.com"),
                new Claim(ClaimTypes.Role, "Driver")
            };
            _driverUser = new ClaimsPrincipal(new ClaimsIdentity(driverClaims, "TestAuth"));

            // Crear controlador
            _controller = new RoutesController(_context, _mockUserManager.Object, _mockLogger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = _adminUser }
                }
            };
        }

        [Fact]
        public async Task SaveRoute_AsAdmin_WithoutDriverId_SavesForSelf()
        {
            // Arrange
            var saveRouteDto = new SaveRouteDto
            {
                RouteName = "Mi Ruta Admin",
                OrderIds = new List<int> { 1, 2, 3 }
            };

            // Act
            var result = await _controller.SaveCurrentRoute(saveRouteDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var savedRoute = await _context.SavedRoutes.FirstOrDefaultAsync(r => r.RouteName == "Mi Ruta Admin");
            savedRoute.Should().NotBeNull();
            savedRoute!.DriverId.Should().Be(1); // Guardado para el admin
        }

        [Fact]
        public async Task SaveRoute_AsAdmin_WithDriverId_SavesForDriver()
        {
            // Arrange
            var saveRouteDto = new SaveRouteDto
            {
                RouteName = "Ruta para Conductor",
                OrderIds = new List<int> { 1, 2 },
                DriverId = 2
            };

            // Act
            var result = await _controller.SaveCurrentRoute(saveRouteDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var savedRoute = await _context.SavedRoutes.FirstOrDefaultAsync(r => r.RouteName == "Ruta para Conductor");
            savedRoute.Should().NotBeNull();
            savedRoute!.DriverId.Should().Be(2); // Guardado para el conductor
        }

        [Fact]
        public async Task SaveRoute_AsDriver_SavesForSelf()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _driverUser }
            };

            var saveRouteDto = new SaveRouteDto
            {
                RouteName = "Mi Ruta Conductor",
                OrderIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await _controller.SaveCurrentRoute(saveRouteDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);

            var savedRoute = await _context.SavedRoutes.FirstOrDefaultAsync(r => r.RouteName == "Mi Ruta Conductor");
            savedRoute.Should().NotBeNull();
            savedRoute!.DriverId.Should().Be(2);
        }

        [Fact]
        public async Task SaveRoute_WithEmptyOrderIds_ReturnsBadRequest()
        {
            // Arrange
            var saveRouteDto = new SaveRouteDto
            {
                RouteName = "Ruta Vacía",
                OrderIds = new List<int>()
            };

            // Act
            var result = await _controller.SaveCurrentRoute(saveRouteDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task SaveRoute_WithInvalidDriverId_ReturnsNotFound()
        {
            // Arrange
            var saveRouteDto = new SaveRouteDto
            {
                RouteName = "Ruta Inválida",
                OrderIds = new List<int> { 1 },
                DriverId = 999
            };

            // Act
            var result = await _controller.SaveCurrentRoute(saveRouteDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetSavedRoutes_AsDriver_ReturnsOnlyOwnRoutes()
        {
            // Arrange
            // Crear rutas guardadas
            var route1 = new SavedRoute
            {
                DriverId = 2,
                RouteName = "Ruta Driver 1",
                OrderIds = JsonSerializer.Serialize(new List<int> { 1, 2 }),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            var route2 = new SavedRoute
            {
                DriverId = 1,
                RouteName = "Ruta Admin",
                OrderIds = JsonSerializer.Serialize(new List<int> { 3 }),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.SavedRoutes.AddRange(route1, route2);
            await _context.SaveChangesAsync();

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _driverUser }
            };

            // Act
            var result = await _controller.GetSavedRoutes();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateRoute_AsAdmin_UpdatesSuccessfully()
        {
            // Arrange
            var savedRoute = new SavedRoute
            {
                DriverId = 1, // Admin's route
                RouteName = "Ruta Original",
                OrderIds = JsonSerializer.Serialize(new List<int> { 1 }),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.SavedRoutes.Add(savedRoute);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateRouteDto
            {
                RouteName = "Ruta Actualizada",
                OrderIds = new List<int> { 1, 2, 3 }
            };

            // Act
            var result = await _controller.UpdateSavedRoute(savedRoute.Id, updateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            
            var updatedRoute = await _context.SavedRoutes.FindAsync(savedRoute.Id);
            updatedRoute!.RouteName.Should().Be("Ruta Actualizada");
        }

        [Fact]
        public async Task AssignRoute_ClonesRouteToDriver()
        {
            // Arrange
            var adminRoute = new SavedRoute
            {
                DriverId = 1,
                RouteName = "Template Admin",
                OrderIds = JsonSerializer.Serialize(new List<int> { 1, 2 }),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.SavedRoutes.Add(adminRoute);
            await _context.SaveChangesAsync();

            var assignDto = new AssignRouteDto
            {
                DriverPhoneNumber = "+573009876543"
            };

            // Act
            var result = await _controller.AssignSavedRoute(adminRoute.Id, assignDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;

            var driverRoutes = await _context.SavedRoutes.Where(r => r.DriverId == 2).ToListAsync();
            driverRoutes.Should().HaveCount(1);
            driverRoutes[0].RouteName.Should().Be("Template Admin");
        }

        [Fact]
        public async Task AssignRoute_WithInvalidPhone_ReturnsNotFound()
        {
            // Arrange
            var adminRoute = new SavedRoute
            {
                DriverId = 1,
                RouteName = "Template",
                OrderIds = JsonSerializer.Serialize(new List<int> { 1 }),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.SavedRoutes.Add(adminRoute);
            await _context.SaveChangesAsync();

            var assignDto = new AssignRouteDto
            {
                DriverPhoneNumber = "+999999999"
            };

            // Act
            var result = await _controller.AssignSavedRoute(adminRoute.Id, assignDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
