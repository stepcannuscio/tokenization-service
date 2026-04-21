using FluentValidation;

namespace Tokenization.Application.Handlers.GetToken;

/// <summary>Validates input for <see cref="GetTokenCommand"/>.</summary>
internal sealed class GetTokenValidator : AbstractValidator<GetTokenCommand>
{
    public GetTokenValidator()
    {
        RuleFor(cmd => cmd.Token).NotEmpty().MaximumLength(128);
    }
}
