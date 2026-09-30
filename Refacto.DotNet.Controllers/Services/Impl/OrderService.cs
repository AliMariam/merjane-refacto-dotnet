using Refacto.DotNet.Controllers.Database.Repositories;
using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Impl
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IReadOnlyDictionary<string, IProductHandler> _handlersByProductType;

        public OrderService(IOrderRepository orderRepository, IEnumerable<IProductHandler> productHandlers)
        {
            _orderRepository = orderRepository;
            _handlersByProductType = productHandlers.ToDictionary(handler => handler.ProductType);
        }

        public async Task<Order?> ProcessOrderAsync(long orderId, CancellationToken cancellationToken = default)
        {
            Order? order = await _orderRepository.FindWithItemsAsync(orderId, cancellationToken);
            if (order is null)
            {
                return null;
            }

            DateTime now = DateTime.Now;
            foreach (Product product in order.Items)
            {
                FindHandler(product)?.Handle(product, now);
            }

            await _orderRepository.SaveChangesAsync(cancellationToken);
            return order;
        }

        // Products of an unknown type are left untouched.
        private IProductHandler? FindHandler(Product product)
        {
            return product.Type is not null && _handlersByProductType.TryGetValue(product.Type, out IProductHandler? handler)
                ? handler
                : null;
        }
    }
}
