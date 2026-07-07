using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Categories.Queries;

public record GetCategoryByIdQuery(int Id) : IRequest<Category?>;
