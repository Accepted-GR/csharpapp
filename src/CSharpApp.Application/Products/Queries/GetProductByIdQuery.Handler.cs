using MediatR;

namespace CSharpApp.Application.Products.Queries
{
	internal class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, GetProductByIdResponse>
	{
		private readonly IProductsService _productsService;

		public GetProductByIdQueryHandler(IProductsService productsService)
		{
			_productsService = productsService;
		}

		public async Task<GetProductByIdResponse> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
		{
			var product = await _productsService.GetProductById(request.Id, cancellationToken);

			return new GetProductByIdResponse(product);
		}
	}
}