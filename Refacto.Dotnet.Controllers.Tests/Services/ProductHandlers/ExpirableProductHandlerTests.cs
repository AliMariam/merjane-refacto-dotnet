using Moq;
using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl.ProductHandlers;

namespace Refacto.Dotnet.Controllers.Tests.Services.ProductHandlers
{
    public class ExpirableProductHandlerTests
    {
        private static readonly DateTime Now = new(2024, 6, 15, 10, 30, 0);

        private readonly Mock<INotificationService> _mockNotificationService = new();
        private readonly ExpirableProductHandler _handler;

        public ExpirableProductHandlerTests()
        {
            _handler = new ExpirableProductHandler(_mockNotificationService.Object);
        }

        [Fact]
        public void Handle_WhenNotExpiredAndInStock_DecrementsStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 30, expiryDate: Now.AddDays(26));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(29, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenExpired_NotifiesExpirationAndMarksUnavailable()
        {
            // GIVEN
            DateTime expiryDate = Now.AddDays(-2);
            Product product = CreateProduct(available: 6, expiryDate: expiryDate);

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenExpiryDateIsToday_TreatsProductAsExpired()
        {
            // GIVEN
            DateTime expiryDate = Now.Date;
            Product product = CreateProduct(available: 6, expiryDate: expiryDate);

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockBeforeExpiry_NotifiesExpiration()
        {
            // GIVEN
            DateTime expiryDate = Now.AddDays(10);
            Product product = CreateProduct(available: 0, expiryDate: expiryDate);

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenExpiryDateIsMissing_Throws()
        {
            // GIVEN
            Product product = CreateProduct(available: 6, expiryDate: null);

            // WHEN
            void Act() => _handler.Handle(product, Now);

            // THEN
            _ = Assert.Throws<InvalidOperationException>(Act);
            Assert.Equal(6, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private static Product CreateProduct(int available, DateTime? expiryDate)
        {
            return new Product
            {
                Available = available,
                LeadTime = 15,
                Type = ProductTypes.Expirable,
                Name = "Milk",
                ExpiryDate = expiryDate
            };
        }
    }
}
