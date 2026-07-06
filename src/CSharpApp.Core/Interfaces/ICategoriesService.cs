 

namespace CSharpApp.Core.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyCollection<Category>> GetCategories(CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryById(int id, CancellationToken cancellationToken = default);
    Task<Category?> CreateCategory(Category category, CancellationToken cancellationToken = default);
}
