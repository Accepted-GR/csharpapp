using System.Net;

namespace ApplicationTests.TestSupport;

// A test inner handler that returns queued responses and records what each outgoing request carried.
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
	private readonly Queue<HttpResponseMessage> _responses;

	public StubHttpMessageHandler(params HttpResponseMessage[] responses)
		=> _responses = new Queue<HttpResponseMessage>(responses);

	public int CallCount { get; private set; }
	public List<string?> AuthorizationParameters { get; } = new();
	public List<string?> Bodies { get; } = new();
	public List<Uri?> RequestUris { get; } = new();

	public static HttpResponseMessage Json(HttpStatusCode status, string json)
		=> new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		CallCount++;
		AuthorizationParameters.Add(request.Headers.Authorization?.Parameter);
		RequestUris.Add(request.RequestUri);
		Bodies.Add(request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

		return _responses.Count > 0 ? _responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.OK);
	}
}
