using MediatR;

namespace CSharpApp.Application.Categories.Queries
{
	public record GetCategoriesQuery() : IRequest<GetCategoriesResponse>;

	public record GetCategoriesResponse
	{
		public IReadOnlyCollection<Category> Categories { get; init; } = [];
	}
}
