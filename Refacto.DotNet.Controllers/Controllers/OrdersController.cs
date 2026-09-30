using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProcessOrderResponse>> ProcessOrder(long orderId, CancellationToken cancellationToken)
        {
            Order? order = await _orderService.ProcessOrderAsync(orderId, cancellationToken);
            if (order is null)
            {
                return NotFound();
            }

            return new ProcessOrderResponse(order.Id);
        }
    }
}
