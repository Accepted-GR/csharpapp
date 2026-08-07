namespace CSharpApp.Core.Interfaces;

public interface ICategoriesService
{
    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">No category has that id.</exception>
    Task<Category> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Category> CreateCategoryAsync(CategoryWriteModel category, CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">No category has that id.</exception>
    Task<Category> UpdateCategoryAsync(int id, CategoryWriteModel category, CancellationToken cancellationToken = default);

    /// <exception cref="UpstreamNotFoundException">No category has that id.</exception>
    Task DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
}
