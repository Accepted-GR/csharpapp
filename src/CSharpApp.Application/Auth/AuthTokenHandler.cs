using System.Net;
using System.Net.Http.Headers;

namespace CSharpApp.Application.Auth
{
	public class AuthTokenHandler : DelegatingHandler
	{
		private readonly IAuthService _authService;

		public AuthTokenHandler(IAuthService authService)
		{
			_authService = authService;
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			await AttachToken(request, cancellationToken);

			var response = await base.SendAsync(request, cancellationToken);
			if (response.StatusCode != HttpStatusCode.Unauthorized)
				return response;

			response.Dispose();
			var token = await _authService.RefreshAccessToken(cancellationToken);

			var retry = await CloneRequest(request, cancellationToken);
			SetBearer(retry, token);
			return await base.SendAsync(retry, cancellationToken);
		}

		private async Task AttachToken(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var token = await _authService.GetAccessToken(cancellationToken);
			SetBearer(request, token);
		}

		private static void SetBearer(HttpRequestMessage request, string token)
			=> request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

		private static async Task<HttpRequestMessage> CloneRequest(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var clone = new HttpRequestMessage(request.Method, request.RequestUri);

			if (request.Content is not null)
			{
				var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
				clone.Content = new ByteArrayContent(bytes);

				foreach (var header in request.Content.Headers)
					clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}

			return clone;
		}
	}
}