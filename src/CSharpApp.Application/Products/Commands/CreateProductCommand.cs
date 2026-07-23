using MediatR;

namespace CSharpApp.Application.Products.Commands
{
	public record CreateProductCommand(CreateProductPayload Payload) : IRequest<int?>;
}