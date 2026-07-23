using System.ComponentModel.DataAnnotations;

namespace CSharpApp.Core.Dtos
{
	public sealed class CreateProductPayload
	{
		[Required]
		public string Title { get; init; } = string.Empty;

		[Required]
		public int Price { get; init; }

		[Required]
		public string Description { get; init; } = string.Empty;

		[Required]
		public int CategoryId { get; init; }

		[Required]
		public IReadOnlyCollection<string> Images { get; init; } = [];
	}
}