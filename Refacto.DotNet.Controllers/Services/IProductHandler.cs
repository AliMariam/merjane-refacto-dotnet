using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services
{
    /// <summary>
    /// Applies the stock rules of one product type when a product of that type is ordered.
    /// </summary>
    public interface IProductHandler
    {
        /// <summary>
        /// The <see cref="Product.Type"/> this handler is responsible for.
        /// </summary>
        string ProductType { get; }

        /// <param name="product">The ordered product.</param>
        /// <param name="now">The moment the order is processed.</param>
        void Handle(Product product, DateTime now);
    }
}
