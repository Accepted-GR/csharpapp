using MediatR;

namespace CSharpApp.Application.Categories.Queries
{
	internal class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, GetCategoryByIdResponse>
	{
		private readonly ICategoriesService _categoriesService;

		public GetCategoryByIdQueryHandler(ICategoriesService categoriesService)
		{
			_categoriesService = categoriesService;
		}

		public async Task<GetCategoryByIdResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
		{
			var category = await _categoriesService.GetCategoryById(request.Id, cancellationToken);

			return new GetCategoryByIdResponse(category);
		}
	}
}
