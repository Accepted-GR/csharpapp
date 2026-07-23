using MediatR;

namespace CSharpApp.Application.Products.Queries
{
	public record GetProductsQuery() : IRequest<GetProductsResponse>;

	public record GetProductsResponse
	{
		public IReadOnlyCollection<Product> Products { get; init; } = [];
	}
}