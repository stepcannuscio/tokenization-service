namespace Tokenization.Api.Logging;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = false)]
internal sealed class SensitiveAttribute(Sensitivity kind = Sensitivity.Secret) : Attribute
{
    public Sensitivity Kind { get; } = kind;
}

internal enum Sensitivity { Pii, Secret, Payment, Credential }