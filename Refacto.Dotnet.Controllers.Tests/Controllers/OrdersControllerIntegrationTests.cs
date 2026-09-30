using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    [Collection("Sequential")]
    public class OrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IServiceScope _scope;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public OrdersControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped(_ => _mockNotificationService.Object);
                });
            });

            _scope = _factory.Services.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _factory.Dispose();
        }

        [Fact]
        public async Task ProcessOrder_ReturnsProcessedOrderId()
        {
            // GIVEN
            Order order = await SaveOrderAsync(
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" });

            // WHEN
            HttpResponseMessage response = await PostProcessOrderAsync(order.Id);

            // THEN
            _ = response.EnsureSuccessStatusCode();
            ProcessOrderResponse? body = await response.Content.ReadFromJsonAsync<ProcessOrderResponse>();
            Assert.Equal(order.Id, body?.id);
        }

        [Fact]
        public async Task ProcessOrder_AppliesStockRulesOfEachProductType()
        {
            // GIVEN
            DateTime milkExpiryDate = DateTime.Now.AddDays(-2);
            Order order = await SaveOrderAsync(
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = milkExpiryDate },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) });

            // WHEN
            HttpResponseMessage response = await PostProcessOrderAsync(order.Id);

            // THEN
            _ = response.EnsureSuccessStatusCode();
            Assert.Equal(29, await GetAvailableAsync("USB Cable"));
            Assert.Equal(0, await GetAvailableAsync("USB Dongle"));
            Assert.Equal(29, await GetAvailableAsync("Butter"));
            Assert.Equal(0, await GetAvailableAsync("Milk"));
            Assert.Equal(29, await GetAvailableAsync("Watermelon"));
            Assert.Equal(30, await GetAvailableAsync("Grapes"));

            _mockNotificationService.Verify(service => service.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.Verify(service => service.SendExpirationNotification("Milk", milkExpiryDate), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_HandlesOutOfStockOutOfSeasonAndUnknownProducts()
        {
            // GIVEN
            DateTime yogurtExpiryDate = DateTime.Now.AddDays(10);
            Order order = await SaveOrderAsync(
                new Product { LeadTime = 0, Available = 0, Type = "NORMAL", Name = "HDMI Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "SEASONAL", Name = "Strawberry", SeasonStartDate = DateTime.Now.AddDays(-5), SeasonEndDate = DateTime.Now.AddDays(30) },
                new Product { LeadTime = 40, Available = 0, Type = "SEASONAL", Name = "Cherry", SeasonStartDate = DateTime.Now.AddDays(-5), SeasonEndDate = DateTime.Now.AddDays(30) },
                new Product { LeadTime = 5, Available = 12, Type = "SEASONAL", Name = "Pumpkin", SeasonStartDate = DateTime.Now.AddDays(-90), SeasonEndDate = DateTime.Now.AddDays(-10) },
                new Product { LeadTime = 5, Available = 0, Type = "EXPIRABLE", Name = "Yogurt", ExpiryDate = yogurtExpiryDate },
                new Product { LeadTime = 5, Available = 5, Type = "DIGITAL", Name = "Gift Card" });

            // WHEN
            HttpResponseMessage response = await PostProcessOrderAsync(order.Id);

            // THEN
            _ = response.EnsureSuccessStatusCode();
            Assert.Equal(0, await GetAvailableAsync("HDMI Cable"));
            Assert.Equal(0, await GetAvailableAsync("Strawberry"));
            Assert.Equal(0, await GetAvailableAsync("Cherry"));
            Assert.Equal(0, await GetAvailableAsync("Pumpkin"));
            Assert.Equal(0, await GetAvailableAsync("Yogurt"));
            Assert.Equal(5, await GetAvailableAsync("Gift Card"));

            _mockNotificationService.Verify(service => service.SendDelayNotification(10, "Strawberry"), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Cherry"), Times.Once());
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Pumpkin"), Times.Once());
            _mockNotificationService.Verify(service => service.SendExpirationNotification("Yogurt", yogurtExpiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_WhenOrderDoesNotExist_ReturnsNotFound()
        {
            // WHEN
            HttpResponseMessage response = await PostProcessOrderAsync(404);

            // THEN
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private async Task<Order> SaveOrderAsync(params Product[] products)
        {
            Order order = new() { Items = products.ToList() };
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return order;
        }

        private Task<HttpResponseMessage> PostProcessOrderAsync(long orderId)
        {
            HttpClient client = _factory.CreateClient();
            return client.PostAsync($"/orders/{orderId}/processOrder", null);
        }

        private async Task<int> GetAvailableAsync(string productName)
        {
            Product product = await _context.Products.AsNoTracking().SingleAsync(p => p.Name == productName);
            return product.Available;
        }
    }
}
