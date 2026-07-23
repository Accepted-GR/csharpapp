using MediatR;

namespace CSharpApp.Application.Products.Queries
{
	public record GetProductByIdQuery(int Id) : IRequest<GetProductByIdResponse>;

	public record GetProductByIdResponse(Product? Product);
}