using Refacto.DotNet.Controllers.Constants;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl.ProductHandlers
{
    /// <summary>
    /// A normal product is sold while in stock; otherwise customers are told the restocking delay.
    /// </summary>
    public class NormalProductHandler : IProductHandler
    {
        private readonly INotificationService _notificationService;

        public NormalProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string ProductType => ProductTypes.Normal;

        public void Handle(Product product, DateTime now)
        {
            if (product.IsInStock)
            {
                product.DecrementStock();
            }
            else if (product.LeadTime > 0)
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.Name!);
            }
        }
    }
}
