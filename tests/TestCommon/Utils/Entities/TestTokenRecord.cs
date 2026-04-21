using Tokenization.Domain.Entities;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Tests.Shared.Utils.ValueObjects;

namespace Tokenization.Tests.Shared.Utils.Entities;

internal static class TestTokenRecord
{
    public static TokenRecord Valid(string? token = null)
    {
        var args = TestCreateTokenArgs.Valid(token);
        var env = TestEncryptedPayload.Valid();
        return args.ToTokenRecord(env);
    }
}