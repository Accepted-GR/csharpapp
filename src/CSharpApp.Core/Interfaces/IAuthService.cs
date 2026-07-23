namespace CSharpApp.Core.Interfaces;

public interface IAuthService
{
	Task<string> GetAccessToken(CancellationToken cancellationToken);

	Task<string> RefreshAccessToken(CancellationToken cancellationToken);
}
