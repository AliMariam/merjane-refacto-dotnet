using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl.ProductHandlers
{
    /// <summary>
    /// A seasonal product is sold only during its season. When it cannot be sold, customers are told
    /// the restocking delay, unless restocking would end after the season: the product is then unavailable.
    /// </summary>
    public class SeasonalProductHandler : IProductHandler
    {
        private readonly INotificationService _notificationService;

        public SeasonalProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string ProductType => ProductTypes.Seasonal;

        public void Handle(Product product, DateTime now)
        {
            if (IsInSeason(product, now.Date) && product.IsInStock)
            {
                product.DecrementStock();
            }
            else if (RestockEndsAfterSeason(product, now))
            {
                _notificationService.SendOutOfStockNotification(product.Name!);
                product.MarkAsUnavailable();
            }
            else if (SeasonHasNotStarted(product, now))
            {
                _notificationService.SendOutOfStockNotification(product.Name!);
            }
            else
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.Name!);
            }
        }

        private static bool IsInSeason(Product product, DateTime today)
        {
            return today > product.SeasonStartDate && today < product.SeasonEndDate;
        }

        private static bool RestockEndsAfterSeason(Product product, DateTime now)
        {
            return now.AddDays(product.LeadTime) > product.SeasonEndDate;
        }

        private static bool SeasonHasNotStarted(Product product, DateTime now)
        {
            return product.SeasonStartDate > now;
        }
    }
}
