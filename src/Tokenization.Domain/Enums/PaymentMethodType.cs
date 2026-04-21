namespace Tokenization.Domain.Enums;

/// <summary>
/// Classifies the primary payment method associated with a token or transaction.
/// </summary>
internal enum PaymentMethodType
{
    /// <summary>General payment card (credit, debit, prepaid).</summary>
    Card,

    /// <summary>Google Pay tokenized credential.</summary>
    GooglePay,

    /// <summary>Apple Pay tokenized credential.</summary>
    ApplePay,

    /// <summary>Alipay wallet/payment method.</summary>
    Alipay,

    /// <summary>WeChat Pay wallet/payment method.</summary>
    WeChatPay,

    /// <summary>PayPal wallet/payment method.</summary>
    Paypal,

    /// <summary>Brazilian boleto bancário voucher method.</summary>
    Boleto,

    /// <summary>Direct bank transfer / account-to-account rails.</summary>
    BankTransfer,

    /// <summary>Any other or custom method not listed above.</summary>
    Other
}