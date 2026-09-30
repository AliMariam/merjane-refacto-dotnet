using Moq;
using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl.ProductHandlers;

namespace Refacto.Dotnet.Controllers.Tests.Services.ProductHandlers
{
    public class NormalProductHandlerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService = new();
        private readonly NormalProductHandler _handler;

        public NormalProductHandlerTests()
        {
            _handler = new NormalProductHandler(_mockNotificationService.Object);
        }

        [Fact]
        public void Handle_WhenInStock_DecrementsStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 30, leadTime: 15);

            // WHEN
            _handler.Handle(product, DateTime.Now);

            // THEN
            Assert.Equal(29, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenLastUnitInStock_SellsIt()
        {
            // GIVEN
            Product product = CreateProduct(available: 1, leadTime: 15);

            // WHEN
            _handler.Handle(product, DateTime.Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStock_NotifiesDelay()
        {
            // GIVEN
            Product product = CreateProduct(available: 0, leadTime: 15);

            // WHEN
            _handler.Handle(product, DateTime.Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendDelayNotification(15, "RJ45 Cable"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockWithoutLeadTime_DoesNotNotify()
        {
            // GIVEN
            Product product = CreateProduct(available: 0, leadTime: 0);

            // WHEN
            _handler.Handle(product, DateTime.Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private static Product CreateProduct(int available, int leadTime)
        {
            return new Product
            {
                Available = available,
                LeadTime = leadTime,
                Type = ProductTypes.Normal,
                Name = "RJ45 Cable"
            };
        }
    }
}
