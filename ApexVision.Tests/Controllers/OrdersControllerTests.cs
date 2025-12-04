using Xunit;
using Moq;
using FluentAssertions;
using ApexVision.Backend.Controllers;
using ApexVision.Backend.Data;
using ApexVision.Backend.DTOs;
using ApexVision.Backend.Models;
using User = ApexVision.Backend.Models.User;
using ApexVision.Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using CloudinaryDotNet.Actions;
using ApexVision.Tests.Helpers;

namespace ApexVision.Tests.Controllers
{
    // Clase de prueba personalizada para anular el comportamiento de GetDriverOrdersAsync
    public class TestableOrdersController : OrdersController
    {
        private readonly List<OrderDto> _testOrders;

        public TestableOrdersController(
            ApplicationDbContext context, 
            IPhotoService photoService, 
            IOptimizationService optimizationService, 
            IAiValidationService aiValidationService, 
            ILogger<OrdersController> logger,
            List<OrderDto> testOrders) 
            : base(context, photoService, optimizationService, aiValidationService, logger)
        {
            _testOrders = testOrders;
        }

        protected override Task<List<OrderDto>> GetDriverOrdersAsync(string userId)
        {
            // Filtrar las órdenes para el conductor específico
            return Task.FromResult(_testOrders.Where(o => o.DriverId.ToString() == userId).ToList());
        }
    }

    public class OrdersControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<IPhotoService> _mockPhotoService;
        private readonly Mock<IOptimizationService> _mockOptimizationService;
        private readonly Mock<IAiValidationService> _mockAiValidationService;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;
        private readonly OrdersController _controller;
        private readonly List<Order> _testOrders;
        private readonly ClaimsPrincipal _adminUser;
        private readonly ClaimsPrincipal _driverUser;

        public OrdersControllerTests()
        {
            // Configurar opciones para el DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
                
            _mockContext = new Mock<ApplicationDbContext>(options);
            _mockPhotoService = new Mock<IPhotoService>();
            _mockOptimizationService = new Mock<IOptimizationService>();
            _mockAiValidationService = new Mock<IAiValidationService>();
            _mockLogger = new Mock<ILogger<OrdersController>>();
            
            // Configurar datos de prueba
            _testOrders = new List<Order>
            {
new Order
                {
                    Id = 1,
                    Address = "Calle 123",
                    Latitude = 6.2442,
                    Longitude = -75.5898,
                    Description = "Pedido de prueba 1",
                    Status = OrderStatus.Pending,
                    RequiresEvidence = false,
                    EvidenceUrl = null,
                    DriverId = null
                },
                new Order
                {
                    Id = 2,
                    Address = "Carrera 456",
                    Latitude = 6.2450,
                    Longitude = -75.5900,
                    Description = "Pedido de prueba 2",
                    Status = OrderStatus.Pending, // Cambiado de Assigned a Pending
                    DriverId = 1,
                    RequiresEvidence = true,
                    EvidenceUrl = null
                }
            };

            // Configurar usuarios de prueba
            var adminClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@apexvision.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.NameIdentifier, "1")
            };
            _adminUser = new ClaimsPrincipal(new ClaimsIdentity(adminClaims, "TestAuth"));

            var driverClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "driver@apexvision.com"),
                new Claim(ClaimTypes.Role, "Driver"),
                new Claim(ClaimTypes.NameIdentifier, "2")
            };
            _driverUser = new ClaimsPrincipal(new ClaimsIdentity(driverClaims, "TestAuth"));

            // Configurar DbContext mock
            var mockOrders = _testOrders.AsQueryable();
            var mockDbSet = new Mock<DbSet<Order>>();
            
            mockDbSet.As<IAsyncEnumerable<Order>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<Order>(mockOrders.GetEnumerator()));
                
            mockDbSet.As<IQueryable<Order>>()
                .Setup(m => m.Provider)
                .Returns(new TestAsyncQueryProvider<Order>(mockOrders.Provider));
                
            mockDbSet.As<IQueryable<Order>>().Setup(m => m.Expression).Returns(mockOrders.Expression);
            mockDbSet.As<IQueryable<Order>>().Setup(m => m.ElementType).Returns(mockOrders.ElementType);
            mockDbSet.As<IQueryable<Order>>().Setup(m => m.GetEnumerator()).Returns(mockOrders.GetEnumerator());
            
            _mockContext.Setup(c => c.Orders).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Orders.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] ids) => _testOrders.FirstOrDefault(o => o.Id == (int)ids[0]));
                
            _mockContext.Setup(c => c.Orders.Add(It.IsAny<Order>())).Callback<Order>(o => 
            {
                o.Id = _testOrders.Max(x => x.Id) + 1;
                _testOrders.Add(o);
            });
            
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1)
                .Callback(() => { });

            // Configurar controlador con usuario administrador por defecto
            // Primero, crear una lista de OrderDto para las pruebas
            var testOrderDtos = _testOrders.Select(o => new OrderDto
            {
                Id = o.Id,
                Description = o.Description,
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                Address = o.Address,
                Status = o.Status,
                RequiresEvidence = o.RequiresEvidence,
                DriverId = o.DriverId,
                EvidenceUrl = o.EvidenceUrl
            }).ToList();

            _controller = new TestableOrdersController(
                _mockContext.Object,
                _mockPhotoService.Object,
                _mockOptimizationService.Object,
                _mockAiValidationService.Object,
                _mockLogger.Object,
                testOrderDtos)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = _adminUser }
                }
            };
        }

        [Fact]
        public async Task CreateOrder_WithValidData_ReturnsOk()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                Address = "Nueva dirección de prueba",
                Latitude = 6.2460,
                Longitude = -75.5910,
                Description = "Nuevo pedido de prueba",
                RequiresEvidence = true
            };

            // Act
            var result = await _controller.CreateOrder(createOrderDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
            var value = okResult.Value as dynamic;
            string message = value?.GetType().GetProperty("message")?.GetValue(value)?.ToString();
            message.Should().Be("Order created successfully");
        }

        [Fact]
        public async Task AssignDriver_WithValidData_ReturnsOk()
        {
            // Arrange
            var orderId = 1;
            var driverId = 2;

            // Act
            var result = await _controller.AssignDriver(orderId, driverId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllOrders_AsAdmin_ReturnsAllOrders()
        {
            // Act
            var result = await _controller.GetAllOrders();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
            orders.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetMyRoute_AsDriver_ReturnsAssignedOrders()
        {
            // Arrange
            // Crear un conductor de prueba con ID 2
            var testDriver = new User 
            { 
                Id = 2, 
                UserName = "driver@apexvision.com",
                FullName = "Driver Test User"
            };

            // Crear una lista de órdenes de prueba
            var testOrders = new List<Order>
            {
                new Order
                {
                    Id = 1,
                    Address = "Calle 123",
                    Latitude = 6.2442,
                    Longitude = -75.5898,
                    Description = "Pedido de prueba 1",
                    Status = OrderStatus.Pending,
                    RequiresEvidence = false,
                    EvidenceUrl = null,
                    DriverId = null
                },
                new Order
                {
                    Id = 2,
                    Address = "Carrera 456",
                    Latitude = 6.2450,
                    Longitude = -75.5900,
                    Description = "Pedido de prueba 2",
                    Status = OrderStatus.Pending,
                    DriverId = 2, // Asignado al conductor con ID 2
                    RequiresEvidence = true,
                    EvidenceUrl = null,
                    Driver = testDriver
                }
            };

            // Convertir a DTOs para la prueba
            var testOrderDtos = testOrders.Select(o => new OrderDto
            {
                Id = o.Id,
                Description = o.Description,
                Latitude = o.Latitude,
                Longitude = o.Longitude,
                Address = o.Address,
                Status = o.Status,
                RequiresEvidence = o.RequiresEvidence,
                DriverId = o.DriverId,
                EvidenceUrl = o.EvidenceUrl
            }).ToList();

            // Configurar el controlador de prueba
            var testController = new TestableOrdersController(
                _mockContext.Object,
                _mockPhotoService.Object,
                _mockOptimizationService.Object,
                _mockAiValidationService.Object,
                _mockLogger.Object,
                testOrderDtos)
            {
                ControllerContext = new ControllerContext
                {
                    // Asegurarse de que el usuario tenga el ID correcto (2)
                    HttpContext = new DefaultHttpContext 
                    { 
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                        {
                            new Claim(ClaimTypes.Name, "driver@apexvision.com"),
                            new Claim(ClaimTypes.Role, "Driver"),
                            new Claim(ClaimTypes.NameIdentifier, "2") // ID del conductor
                        }))
                    }
                }
            };

            // Act
            var result = await testController.GetMyRoute();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var orders = okResult.Value.Should().BeAssignableTo<List<OrderDto>>().Subject;
            orders.Should().NotBeNull();
            // Verificar que solo se devuelva la orden asignada al conductor
            orders.Should().HaveCount(1);
            orders[0].DriverId.Should().Be(2);
        }

        [Fact]
        public async Task CompleteOrder_WithValidData_ReturnsOk()
        {
            // Arrange
            var orderId = 1;
            var fileMock = new Mock<IFormFile>();
            var content = "Fake file content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
            ms.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);

            _mockPhotoService.Setup(x => x.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult { SecureUrl = new Uri("http://example.com/test.jpg") });
            _mockAiValidationService.Setup(x => x.ValidateDeliveryEvidenceAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Configurar como usuario conductor
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _driverUser }
            };

            // Act
            var result = await _controller.CompleteOrder(orderId, fileMock.Object);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            _testOrders.First(o => o.Id == orderId).Status.Should().Be(OrderStatus.Completed);
        }

        [Fact]
        public async Task CompleteOrder_WithInvalidOrder_ReturnsNotFound()
        {
            // Arrange
            var orderId = 999; // ID que no existe
            var fileMock = new Mock<IFormFile>();

            // Act
            var result = await _controller.CompleteOrder(orderId, fileMock.Object);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().Be("Order not found.");
        }

        [Fact]
        public async Task CompleteOrder_RequiresEvidenceButNoFile_ReturnsBadRequest()
        {
            // Arrange
            var orderId = 1;
            _testOrders[0].RequiresEvidence = true;

            // Act
            var result = await _controller.CompleteOrder(orderId, null);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Evidence file is required for this order.");
        }

        [Fact]
        public async Task CompleteOrder_WithInvalidEvidence_ReturnsBadRequest()
        {
            // Arrange
            var orderId = 1;
            var fileMock = new Mock<IFormFile>();
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            await writer.WriteAsync("Fake file content");
            await writer.FlushAsync();
            ms.Position = 0;

            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns("test.jpg");
            fileMock.Setup(_ => _.Length).Returns(ms.Length);

            _mockPhotoService.Setup(x => x.AddPhotoAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync(new ImageUploadResult { SecureUrl = new Uri("http://example.com/test.jpg") });
            _mockAiValidationService.Setup(x => x.ValidateDeliveryEvidenceAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _testOrders[0].RequiresEvidence = true;

            // Act
            var result = await _controller.CompleteOrder(orderId, fileMock.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Invalid evidence. The image does not seem to be a valid delivery evidence.");
        }

        [Fact]
        public async Task OptimizeMyRoute_AsDriver_ReturnsOk()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _driverUser }
            };

            _mockOptimizationService.Setup(x => x.OptimizeRouteAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.OptimizeMyRoute();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task OptimizeMyRoute_ServiceUnavailable_ReturnsServiceUnavailable()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _driverUser }
            };

            _mockOptimizationService.Setup(x => x.OptimizeRouteAsync(It.IsAny<string>()))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            // Act
            var result = await _controller.OptimizeMyRoute();

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(503);
            statusResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateOrder_WithInvalidCoordinates_ReturnsBadRequest()
        {
            // Arrange
            var createOrderDto = new CreateOrderDto
            {
                Address = "Dirección inválida",
                Latitude = 0,
                Longitude = 0,
                Description = "Pedido con coordenadas inválidas"
            };

            // Act
            var result = await _controller.CreateOrder(createOrderDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Coordinates (0,0) are not allowed.");
        }
    }
}
