namespace CSharpApp.Core.Interfaces;

public interface IAuthApiClient
{
	Task<AuthTokens> Login(CancellationToken cancellationToken);

	Task<AuthTokens?> Refresh(string refreshToken, CancellationToken cancellationToken);
}
