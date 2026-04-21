using FluentValidation;

namespace Tokenization.Application.Handlers.DetokenizeToken;

/// <summary>Validates input for <see cref="DetokenizeTokenCommand"/>.</summary>
internal sealed class DetokenizeTokenValidator : AbstractValidator<DetokenizeTokenCommand>
{
    public DetokenizeTokenValidator()
    {
        RuleFor(cmd => cmd.Token).NotEmpty().MaximumLength(128);
    }
}
