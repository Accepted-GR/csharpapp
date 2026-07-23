using MediatR;

namespace CSharpApp.Application.Categories.Queries
{
	public record GetCategoryByIdQuery(int Id) : IRequest<GetCategoryByIdResponse>;

	public record GetCategoryByIdResponse(Category? Category);
}
