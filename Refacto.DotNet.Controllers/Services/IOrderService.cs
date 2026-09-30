using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services
{
    public interface IOrderService
    {
        /// <summary>
        /// Applies the stock rules of each ordered product and saves the result.
        /// </summary>
        /// <returns>The processed order, or <c>null</c> when no order has this id.</returns>
        Task<Order?> ProcessOrderAsync(long orderId, CancellationToken cancellationToken = default);
    }
}
