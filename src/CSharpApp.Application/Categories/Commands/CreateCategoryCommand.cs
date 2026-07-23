using MediatR;

namespace CSharpApp.Application.Categories.Commands
{
	public record CreateCategoryCommand(CreateCategoryPayload Payload) : IRequest<int?>;
}
