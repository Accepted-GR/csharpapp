using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Products.Queries;

public record GetProductsQuery(int? Offset, int? Limit) : IRequest<IReadOnlyCollection<Product>>;
