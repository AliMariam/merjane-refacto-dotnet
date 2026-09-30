using Moq;
using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Database.Repositories;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl;

namespace Refacto.Dotnet.Controllers.Tests.Services
{
    public class OrderServiceTests
    {
        private const long OrderId = 42;

        private readonly Mock<IOrderRepository> _mockOrderRepository = new();
        private readonly Mock<IProductHandler> _mockNormalHandler = CreateHandler(ProductTypes.Normal);
        private readonly Mock<IProductHandler> _mockSeasonalHandler = CreateHandler(ProductTypes.Seasonal);
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _orderService = new OrderService(
                _mockOrderRepository.Object,
                new[] { _mockNormalHandler.Object, _mockSeasonalHandler.Object });
        }

        [Fact]
        public async Task ProcessOrderAsync_WhenOrderDoesNotExist_ReturnsNullWithoutSaving()
        {
            // GIVEN
            _ = _mockOrderRepository.Setup(repository => repository.FindWithItemsAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Order?)null);

            // WHEN
            Order? result = await _orderService.ProcessOrderAsync(OrderId);

            // THEN
            Assert.Null(result);
            _mockOrderRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Fact]
        public async Task ProcessOrderAsync_HandlesEachProductWithTheHandlerOfItsTypeThenSaves()
        {
            // GIVEN
            Product normalProduct = new() { Type = ProductTypes.Normal, Name = "USB Cable" };
            Product seasonalProduct = new() { Type = ProductTypes.Seasonal, Name = "Watermelon" };
            Order order = GivenOrder(normalProduct, seasonalProduct);

            // WHEN
            Order? result = await _orderService.ProcessOrderAsync(OrderId);

            // THEN
            Assert.Same(order, result);
            _mockNormalHandler.Verify(handler => handler.Handle(normalProduct, It.IsAny<DateTime>()), Times.Once());
            _mockNormalHandler.Verify(handler => handler.Handle(seasonalProduct, It.IsAny<DateTime>()), Times.Never());
            _mockSeasonalHandler.Verify(handler => handler.Handle(seasonalProduct, It.IsAny<DateTime>()), Times.Once());
            _mockSeasonalHandler.Verify(handler => handler.Handle(normalProduct, It.IsAny<DateTime>()), Times.Never());
            _mockOrderRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task ProcessOrderAsync_WhenAProductCannotBeHandled_SavesNothing()
        {
            // GIVEN
            _ = GivenOrder(new Product { Type = ProductTypes.Normal, Name = "USB Cable" });
            _ = _mockNormalHandler.Setup(handler => handler.Handle(It.IsAny<Product>(), It.IsAny<DateTime>()))
                .Throws<InvalidOperationException>();

            // WHEN
            Task Act() => _orderService.ProcessOrderAsync(OrderId);

            // THEN
            _ = await Assert.ThrowsAsync<InvalidOperationException>(Act);
            _mockOrderRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }

        [Theory]
        [InlineData("DIGITAL")]
        [InlineData(null)]
        public async Task ProcessOrderAsync_LeavesProductsOfUnknownTypeUntouched(string? productType)
        {
            // GIVEN
            Product product = new() { Type = productType, Name = "Gift Card", Available = 5 };
            _ = GivenOrder(product);

            // WHEN
            _ = await _orderService.ProcessOrderAsync(OrderId);

            // THEN
            Assert.Equal(5, product.Available);
            _mockNormalHandler.Verify(handler => handler.Handle(It.IsAny<Product>(), It.IsAny<DateTime>()), Times.Never());
            _mockSeasonalHandler.Verify(handler => handler.Handle(It.IsAny<Product>(), It.IsAny<DateTime>()), Times.Never());
            _mockOrderRepository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        private Order GivenOrder(params Product[] products)
        {
            Order order = new() { Id = OrderId, Items = products.ToList() };
            _ = _mockOrderRepository.Setup(repository => repository.FindWithItemsAsync(OrderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);
            return order;
        }

        private static Mock<IProductHandler> CreateHandler(string productType)
        {
            Mock<IProductHandler> handler = new();
            _ = handler.Setup(h => h.ProductType).Returns(productType);
            return handler;
        }
    }
}
