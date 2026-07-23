using MediatR;

namespace CSharpApp.Application.Products.Queries
{
	internal class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, GetProductsResponse>
	{
		private readonly IProductsService _productsService;

		public GetProductsQueryHandler(IProductsService productsService)
		{
			_productsService = productsService;
		}

		public async Task<GetProductsResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
		{
			var products = await _productsService.GetProducts();

			return new GetProductsResponse()
			{
				Products = products,
			};
		}
	}
}