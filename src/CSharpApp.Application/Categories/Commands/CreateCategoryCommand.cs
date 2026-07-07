using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Categories.Commands;

public record CreateCategoryCommand(Category Category) : IRequest<Category?>;
