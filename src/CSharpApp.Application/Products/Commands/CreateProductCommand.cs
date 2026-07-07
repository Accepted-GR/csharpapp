using MediatR;
using CSharpApp.Core.Dtos;

namespace CSharpApp.Application.Products.Commands;

public record CreateProductCommand(Product Product) : IRequest<Product?>;
