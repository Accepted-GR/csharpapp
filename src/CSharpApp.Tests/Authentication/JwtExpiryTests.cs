namespace CSharpApp.Tests.Authentication;

public sealed class JwtExpiryTests
{
    [Fact]
    public void TryRead_ReturnsExpiry_ForAValidToken()
    {
        var expected = DateTimeOffset.FromUnixTimeSeconds(1_787_735_027);

        var token = TokenBuilder.WithClaims($"{{\"sub\":1,\"exp\":{expected.ToUnixTimeSeconds()}}}");

        Assert.Equal(expected, JwtExpiry.TryRead(token));
    }

    [Fact]
    public void TryRead_HandlesPayloadsNeedingBase64UrlPadding()
    {
        // Base64url strips '=' padding, and a payload whose length is not a
        // multiple of four is the case a hand-rolled decoder usually gets wrong.
        // Looping over lengths guarantees all three padding cases are covered.
        for (var filler = 0; filler < 6; filler++)
        {
            var expected = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
            var padding = new string('x', filler);

            var token = TokenBuilder.WithClaims(
                $"{{\"pad\":\"{padding}\",\"exp\":{expected.ToUnixTimeSeconds()}}}");

            Assert.Equal(expected, JwtExpiry.TryRead(token));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-jwt")]
    [InlineData("only.two")]
    [InlineData("a.b.c.d")]
    public void TryRead_ReturnsNull_ForStructurallyInvalidTokens(string? token)
    {
        Assert.Null(JwtExpiry.TryRead(token));
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenThePayloadIsNotBase64()
    {
        Assert.Null(JwtExpiry.TryRead("header.!!!not-base64!!!.signature"));
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenThePayloadIsNotJson()
    {
        Assert.Null(JwtExpiry.TryRead(TokenBuilder.WithClaims("this is not json")));
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenThereIsNoExpClaim()
    {
        Assert.Null(JwtExpiry.TryRead(TokenBuilder.WithClaims("{\"sub\":1}")));
    }

    [Fact]
    public void TryRead_ReturnsNull_WhenExpIsNotANumber()
    {
        // A caller that trusted a non-numeric exp would cache a token forever or
        // never at all, so this must degrade to "unknown" rather than throw.
        Assert.Null(JwtExpiry.TryRead(TokenBuilder.WithClaims("{\"exp\":\"soon\"}")));
    }
}
