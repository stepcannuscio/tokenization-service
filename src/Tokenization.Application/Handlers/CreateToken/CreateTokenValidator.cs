using FluentValidation;
using Tokenization.Domain.Enums;

namespace Tokenization.Application.Handlers.CreateToken;

/// <summary>
/// Validates input for <see cref="CreateTokenCommand"/>. Avoids PCI leakage by not logging values.
/// </summary>
internal sealed class CreateTokenValidator : AbstractValidator<CreateTokenCommand>
{
    public CreateTokenValidator()
    {
        RuleFor(cmd => cmd.TenantId)
            .NotEmpty()
            .MaximumLength(128);
        
        RuleFor(cmd => cmd.CustomerId)
            .NotEmpty()
            .MaximumLength(128);
        
        RuleFor(cmd => cmd.PaymentMethodType)
            .NotEmpty()
            .MaximumLength(32)
            .IsEnumName(typeof(PaymentMethodType));
        
        RuleFor(cmd => cmd.TokenType)
            .NotEmpty()
            .MaximumLength(32)
            .IsEnumName(typeof(TokenType));
        
        RuleFor(cmd => cmd.StoredCredentialInitiator)
            .MaximumLength(32)
            .IsEnumName(typeof(StoredCredentialInitiator))
            .When(cmd => !string.IsNullOrEmpty(cmd.StoredCredentialInitiator));
        
        RuleFor(cmd => cmd.StoredCredentialReason)
            .MaximumLength(32)
            .IsEnumName(typeof(StoredCredentialReason))
            .When(cmd => !string.IsNullOrEmpty(cmd.StoredCredentialReason));

        RuleFor(cmd => cmd.Network)
            .MaximumLength(32)
            .When(cmd => !string.IsNullOrEmpty(cmd.Network));
        
        RuleFor(cmd => cmd.Currency)
            .Length(3)
            .When(cmd => !string.IsNullOrEmpty(cmd.Currency));
        
        RuleFor(cmd => cmd.Country)
            .Length(2)
            .When(cmd => !string.IsNullOrEmpty(cmd.Country));
        
        RuleFor(cmd => cmd.ExpiresAt)
            .GreaterThan(DateTimeOffset.Now)
            .When(cmd => cmd.ExpiresAt.HasValue);
        
        RuleFor(cmd => cmd.MaxUses)
            .GreaterThan(0)
            .When(cmd => cmd.MaxUses.HasValue);
        
        RuleFor(cmd => cmd.InitialTransactionId)
            .MaximumLength(128)
            .When(cmd => !string.IsNullOrEmpty(cmd.InitialTransactionId));


        When(cmd => cmd.Card != null, () =>
        {
            RuleFor(cmd => cmd.Card).ChildRules(card =>
            {
                card.RuleFor(c => c!.Pan)
                    .NotEmpty()
                    .CreditCard();

                card.RuleFor(c => c!.ExpMonth)
                    .InclusiveBetween(1, 12);

                card.RuleFor(c => c!.ExpYear)
                    .InclusiveBetween(DateTime.UtcNow.Year, DateTime.UtcNow.Year + 25);

                card.RuleFor(c => c!.CardholderName)
                    .MaximumLength(128)
                    .When(c => !string.IsNullOrEmpty(c?.CardholderName));
            });
        });
    }
}