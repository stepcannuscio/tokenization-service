using FluentAssertions;
using FluentValidation;
using MediatR;
using Tokenization.Application.Handlers;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Handlers;

public class ValidationBehaviorTests
{
    private sealed record Ping(string Message) : IRequest<string>;

    private sealed class PassingValidator : AbstractValidator<Ping>
    {
        public PassingValidator()
        {
            RuleFor(x => x.Message).NotEmpty();
        }
    }

    private sealed class FailingValidator : AbstractValidator<Ping>
    {
        public FailingValidator()
        {
            RuleFor(p => p.Message).Must(_ => false).WithMessage("nope");
        }
    }

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<Ping, string>([]);
        var called = false;

        Task<string> Next(CancellationToken _)
        {
            called = true;
            return Task.FromResult("ok");
        }

        var result = await behavior.Handle(new Ping("hi"), Next, CancellationToken.None);

        called.Should().BeTrue();
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_AllPass_CallsNext()
    {
        var validators = new IValidator<Ping>[] { new PassingValidator() };
        var behavior = new ValidationBehavior<Ping, string>(validators);
        var result = await behavior.Handle(new Ping("hi"), _ => Task.FromResult("ok"), CancellationToken.None);
        result.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_Failures_ThrowsValidationException()
    {
        var validators = new IValidator<Ping>[] { new FailingValidator() };
        var behavior = new ValidationBehavior<Ping, string>(validators);

        var act = () => behavior.Handle(new Ping("anything"), _ => Task.FromResult("never"), CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage == "nope"));
    }
}
