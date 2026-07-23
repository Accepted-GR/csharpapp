using MediatR;

namespace CSharpApp.Application.Products.Commands
{
	internal class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int?>
	{
		private readonly IProductsService _productsService;
		private readonly ILogger<CreateProductCommandHandler> _logger;

		public CreateProductCommandHandler(IProductsService productsService, ILogger<CreateProductCommandHandler> logger)
		{
			_productsService = productsService;
			_logger = logger;
		}

		public async Task<int?> Handle(CreateProductCommand request, CancellationToken cancellationToken)
		{
			var productId = await _productsService.CreateProduct(request.Payload, cancellationToken);

			if (productId is not null)
				_logger.LogInformation("Product was created with Id: {Id}", productId);

			return productId;
		}
	}
}