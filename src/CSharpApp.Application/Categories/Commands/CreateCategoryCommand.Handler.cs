using MediatR;

namespace CSharpApp.Application.Categories.Commands
{
	internal class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, int?>
	{
		private readonly ICategoriesService _categoriesService;
		private readonly ILogger<CreateCategoryCommandHandler> _logger;

		public CreateCategoryCommandHandler(ICategoriesService categoriesService, ILogger<CreateCategoryCommandHandler> logger)
		{
			_categoriesService = categoriesService;
			_logger = logger;
		}

		public async Task<int?> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
		{
			var categoryId = await _categoriesService.CreateCategory(request.Payload, cancellationToken);

			if (categoryId is not null)
				_logger.LogInformation("Category was created with Id: {Id}", categoryId);

			return categoryId;
		}
	}
}
