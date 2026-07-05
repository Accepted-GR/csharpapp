namespace CSharpApp.Core.Interfaces;

public interface ITokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);
}
