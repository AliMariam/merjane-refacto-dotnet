using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl.ProductHandlers
{
    /// <summary>
    /// An expirable product is sold while in stock and not expired; otherwise it becomes unavailable.
    /// </summary>
    public class ExpirableProductHandler : IProductHandler
    {
        private readonly INotificationService _notificationService;

        public ExpirableProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string ProductType => ProductTypes.Expirable;

        public void Handle(Product product, DateTime now)
        {
            DateTime expiryDate = product.ExpiryDate
                ?? throw new InvalidOperationException($"Expirable product '{product.Name}' has no expiry date.");

            if (product.IsInStock && expiryDate > now.Date)
            {
                product.DecrementStock();
            }
            else
            {
                // Legacy behavior: an out-of-stock product is reported as expired too, even before its expiry date.
                _notificationService.SendExpirationNotification(product.Name!, expiryDate);
                product.MarkAsUnavailable();
            }
        }
    }
}
