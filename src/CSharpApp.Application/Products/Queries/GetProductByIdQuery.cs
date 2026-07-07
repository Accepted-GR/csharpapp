using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Products.Queries;

public record GetProductByIdQuery(int Id) : IRequest<Product?>;
