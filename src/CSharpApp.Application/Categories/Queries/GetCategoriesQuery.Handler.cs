using MediatR;

namespace CSharpApp.Application.Categories.Queries
{
	internal class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, GetCategoriesResponse>
	{
		private readonly ICategoriesService _categoriesService;

		public GetCategoriesQueryHandler(ICategoriesService categoriesService)
		{
			_categoriesService = categoriesService;
		}

		public async Task<GetCategoriesResponse> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
		{
			var categories = await _categoriesService.GetCategories(cancellationToken);

			return new GetCategoriesResponse()
			{
				Categories = categories,
			};
		}
	}
}
