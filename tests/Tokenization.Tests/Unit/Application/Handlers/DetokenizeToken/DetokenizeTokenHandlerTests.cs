using FluentAssertions;
using Moq;
using Tokenization.Application.Handlers.DetokenizeToken;
using Tokenization.Domain.Abstractions;
using Tokenization.Tests.Shared.Utils.Commands;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.DetokenizeToken;

public class DetokenizeTokenHandlerTests
{
    [Fact]
    public async Task Handle_CallsService_AndMaps()
    {
        // Arrange
        var cmd = TestDetokenizeTokenCommand.Valid();
        var detokenizeToken = TestDetokenizedToken.Valid();
        var summary = TestTokenSummary.Valid();

        var svc = new Mock<ITokenService>(MockBehavior.Strict);
        svc.Setup(s => s.RedeemTokenAsync(cmd.Token, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        svc.Setup(s => s.DetokenizeTokenAsync(cmd.Token, It.IsAny<CancellationToken>())).ReturnsAsync(detokenizeToken);

        var handler = new DetokenizeTokenHandler(svc.Object);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(detokenizeToken);
        svc.VerifyAll();
    }
}