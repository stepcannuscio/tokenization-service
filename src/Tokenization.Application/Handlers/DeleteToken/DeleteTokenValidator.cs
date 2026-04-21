using FluentValidation;

namespace Tokenization.Application.Handlers.DeleteToken;

/// <summary>Validates input for <see cref="DeleteTokenCommand"/>.</summary>
internal sealed class DeleteTokenValidator : AbstractValidator<DeleteTokenCommand>
{
    public DeleteTokenValidator()
    {
        RuleFor(cmd => cmd.Token)
            .NotEmpty()
            .MaximumLength(128);
    }
}