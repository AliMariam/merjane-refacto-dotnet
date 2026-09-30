using Moq;
using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Impl.ProductHandlers;

namespace Refacto.Dotnet.Controllers.Tests.Services.ProductHandlers
{
    public class SeasonalProductHandlerTests
    {
        private static readonly DateTime Now = new(2024, 6, 15, 10, 30, 0);

        private readonly Mock<INotificationService> _mockNotificationService = new();
        private readonly SeasonalProductHandler _handler;

        public SeasonalProductHandlerTests()
        {
            _handler = new SeasonalProductHandler(_mockNotificationService.Object);
        }

        [Fact]
        public void Handle_WhenInSeasonAndInStock_DecrementsStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 30, leadTime: 15, seasonStart: Now.AddDays(-2), seasonEnd: Now.AddDays(58));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(29, product.Available);
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenInSeasonAndOutOfStockWithRestockBeforeSeasonEnd_NotifiesDelay()
        {
            // GIVEN
            Product product = CreateProduct(available: 0, leadTime: 10, seasonStart: Now.AddDays(-5), seasonEnd: Now.AddDays(30));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendDelayNotification(10, "Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenInSeasonAndOutOfStockWithRestockAfterSeasonEnd_NotifiesOutOfStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 0, leadTime: 40, seasonStart: Now.AddDays(-5), seasonEnd: Now.AddDays(30));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenRestockArrivesOnSeasonEndDate_NotifiesOutOfStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 0, leadTime: 30, seasonStart: Now.AddDays(-5), seasonEnd: Now.Date.AddDays(30));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonStartsToday_NotifiesDelayInsteadOfSelling()
        {
            // GIVEN
            // Legacy behavior: the season start date is exclusive (to be confirmed with the business).
            Product product = CreateProduct(available: 30, leadTime: 5, seasonStart: Now.Date, seasonEnd: Now.AddDays(30));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(30, product.Available);
            _mockNotificationService.Verify(service => service.SendDelayNotification(5, "Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonIsOver_NotifiesOutOfStockAndMarksUnavailable()
        {
            // GIVEN
            Product product = CreateProduct(available: 12, leadTime: 5, seasonStart: Now.AddDays(-90), seasonEnd: Now.AddDays(-10));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonEndsToday_NotifiesOutOfStockAndMarksUnavailable()
        {
            // GIVEN
            Product product = CreateProduct(available: 12, leadTime: 5, seasonStart: Now.AddDays(-90), seasonEnd: Now.Date);

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonHasNotStarted_NotifiesOutOfStockAndKeepsStock()
        {
            // GIVEN
            Product product = CreateProduct(available: 30, leadTime: 15, seasonStart: Now.AddDays(180), seasonEnd: Now.AddDays(240));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(30, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonHasNotStartedAndRestockEndsAfterSeason_MarksUnavailable()
        {
            // GIVEN
            Product product = CreateProduct(available: 30, leadTime: 300, seasonStart: Now.AddDays(180), seasonEnd: Now.AddDays(240));

            // WHEN
            _handler.Handle(product, Now);

            // THEN
            Assert.Equal(0, product.Available);
            _mockNotificationService.Verify(service => service.SendOutOfStockNotification("Watermelon"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private static Product CreateProduct(int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product
            {
                Available = available,
                LeadTime = leadTime,
                Type = ProductTypes.Seasonal,
                Name = "Watermelon",
                SeasonStartDate = seasonStart,
                SeasonEndDate = seasonEnd
            };
        }
    }
}
