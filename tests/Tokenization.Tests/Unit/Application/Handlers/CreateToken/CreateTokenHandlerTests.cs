using FluentAssertions;
using Moq;
using Tokenization.Application.Handlers.CreateToken;
using Tokenization.Domain.Abstractions;
using Tokenization.Tests.Shared.Utils.Commands;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.CreateToken;

public class CreateTokenHandlerTests
{
    [Fact]
    public async Task Handle_MapsArgs_DelegatesToService_And_ReturnsSummary()
    {
        // Arrange
        var cmd = TestCreateTokenCommand.Valid();
        var summary = TestTokenSummary.Valid();
        
        var svc = new Mock<ITokenService>(MockBehavior.Strict);
        var args = cmd.ToCreateTokenArgs();
        var payload = cmd.ToSensitivePayload();
        svc.Setup(s => s.IssueTokenAsync(args, It.IsAny<ReadOnlyMemory<byte>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        
        var handler = new CreateTokenHandler(svc.Object);
        
        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(summary);
        svc.VerifyAll();
    }
}