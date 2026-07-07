using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Categories.Queries;

public record GetCategoriesQuery : IRequest<IReadOnlyCollection<Category>>;
