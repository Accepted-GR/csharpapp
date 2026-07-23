using System.ComponentModel.DataAnnotations;

namespace CSharpApp.Core.Dtos
{
	public sealed class CreateCategoryPayload
	{
		[Required]
		public string Name { get; init; } = string.Empty;

		[Required]
		public string Image { get; init; } = string.Empty;
	}
}
