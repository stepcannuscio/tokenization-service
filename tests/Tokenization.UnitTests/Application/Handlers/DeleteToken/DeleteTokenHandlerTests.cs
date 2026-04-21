using FluentAssertions;
using Moq;
using Tokenization.Application.Handlers.DeleteToken;
using Tokenization.Domain.Abstractions;
using Tokenization.Tests.Shared.Utils.Commands;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers.DeleteToken;

public class DeleteTokenHandlerTests
{
    [Fact]
    public async Task Handle_CallsService_AndReturnsTrue()
    {
        var cmd = TestDeleteTokenCommand.Valid();
        var svc = new Mock<ITokenService>(MockBehavior.Strict);
        svc.Setup(s => s.DeleteTokenAsync(cmd.Token, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new DeleteTokenHandler(svc.Object);
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeTrue();
        svc.VerifyAll();
    }
}
