namespace CSharpApp.Core.Interfaces;

public interface ICategoriesService
{
	Task<IReadOnlyCollection<Category>> GetCategories(CancellationToken cancellationToken);

	Task<Category?> GetCategoryById(int id, CancellationToken cancellationToken);

	Task<int?> CreateCategory(CreateCategoryPayload payload, CancellationToken cancellationToken);
}
