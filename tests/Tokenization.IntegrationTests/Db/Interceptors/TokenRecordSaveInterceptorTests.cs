using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tokenization.Infrastructure.Db.Constants;
using Tokenization.Infrastructure.Db.Mapping.TokenRecord;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Integration.Db.Interceptors;

public class TokenRecordSaveInterceptorTests(SqlServerFixture sqlFixture) : IClassFixture<SqlServerFixture>
{
    [Fact]
    public async Task Saving_Populates_BlindIndexes_And_Timestamps()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var db = dbWrap.Context;
        var args = TestCreateTokenArgs.Valid(Guid.NewGuid().ToString());
        var env = TestEncryptedPayload.Valid();
        var entity = args.ToTokenRecord(env);

        db.Tokens.Add(entity);
        await db.SaveChangesAsync();

        // Verify shadow properties set via EF.Property
        var saved = await db.Tokens.SingleAsync(t => t.Token == args.Token);
        var tenantHash = db.Entry(saved).Property<byte[]>(ShadowProperties.TenantHash).CurrentValue;
        var customerHash = db.Entry(saved).Property<byte[]>(ShadowProperties.CustomerHash);

        tenantHash.Should().NotBeNull();
        customerHash.Should().NotBeNull();
        saved.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
    }
}
