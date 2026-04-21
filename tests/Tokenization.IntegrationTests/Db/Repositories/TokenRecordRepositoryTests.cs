using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tokenization.Infrastructure.Db.Constants;
using Tokenization.Infrastructure.Db.Repositories;
using Tokenization.Infrastructure.Db.Services;
using Tokenization.Tests.Shared.Fixtures;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Integration.Db.Repositories;

public class TokenRecordRepositoryTests(SqlServerFixture sqlFixture) : IClassFixture<SqlServerFixture>
{
    private static readonly ILogger<BulkOperationsService> BulkLogger =
        new Mock<ILogger<BulkOperationsService>>().Object;

    [Fact]
    public async Task Create_ThrowsException_When_Args_Is_Null()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repo.CreateAsync(null!, TestEncryptedPayload.Valid()));
    }

    [Fact]
    public async Task Create_ThrowsException_When_Payload_Is_Null()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repo.CreateAsync(TestCreateTokenArgs.Valid("test"), null!));
    }

    [Fact]
    public async Task Create_Persists_And_Sets_BlindIndexes()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var db = dbWrap.Context;
        var repo = new TokenRecordRepository(db, dbWrap.Blind, new BulkOperationsService(db, BulkLogger));
        var args = TestCreateTokenArgs.Valid("tok-1");
        var env = TestEncryptedPayload.Valid();

        var token = await repo.CreateAsync(args, env);

        token.Token.Should().Be("tok-1");

        var saved = await db.Tokens.AsNoTracking().SingleAsync(t => t.Token == "tok-1");
        db.Entry(saved).Property<byte[]>(ShadowProperties.TenantHash).Should().NotBeNull();
        db.Entry(saved).Property<byte[]>(ShadowProperties.CustomerHash).Should().NotBeNull();
    }

    [Fact]
    public async Task GetSummaryByToken_Returns_Projection()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-2"), TestEncryptedPayload.Valid());

        var dto = await repo.GetSummaryByTokenAsync("tok-2");

        dto.Should().NotBeNull();
        dto.Token.Should().Be("tok-2");
        dto.Last4.Should().Be("1111");
    }

    [Fact]
    public async Task GetSummaryByToken_ReturnsNull_When_Token_Not_Found()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.GetSummaryByTokenAsync("non-existent-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSummaryByToken_ThrowsException_When_Token_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetSummaryByTokenAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetSummaryByTokenAsync(null!));
    }

    [Fact]
    public async Task FindByTenantCustomer_ThrowsException_When_TenantId_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync("", "customer-123"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync(null!, "customer-123"));
    }

    [Fact]
    public async Task FindByTenantCustomer_ThrowsException_When_CustomerId_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync("tenant-123", ""));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync("tenant-123", null!));
    }

    [Fact]
    public async Task FindByTenantCustomer_ThrowsException_When_Take_Is_Invalid()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync("tenant-123", "customer-123", take: 0));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.FindByTenantCustomerAsync("tenant-123", "customer-123", take: -1));
    }

    [Fact]
    public async Task FindByTenantCustomer_ReturnsEmpty_When_No_Matching_Tokens()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.FindByTenantCustomerAsync("non-existent-tenant", "non-existent-customer");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindByTenantCustomer_Uses_BlindIndexes()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-A"), TestEncryptedPayload.Valid());
        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-B"), TestEncryptedPayload.Valid());

        var list = await repo.FindByTenantCustomerAsync("tenant-123", "customer-789", take: 10);

        list.Should().NotBeEmpty();
        list.Select(x => x.Token).Should().Contain(["tok-A", "tok-B"]);
    }

    [Fact]
    public async Task FindByTenantCustomer_Respects_Take_Parameter()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        // Create 5 tokens with same tenant/customer
        for (int i = 1; i <= 5; i++)
        {
            await repo.CreateAsync(TestCreateTokenArgs.Valid($"limit-test-{i}"), TestEncryptedPayload.Valid());
        }

        var result = await repo.FindByTenantCustomerAsync("tenant-123", "customer-789", take: 3);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetEncryptedPayload_ReturnsNull_When_Token_Not_Found()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.GetEncryptedPayloadAsync("non-existent-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetEncryptedPayload_ThrowsException_When_Token_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetEncryptedPayloadAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetEncryptedPayloadAsync(null!));
    }

    [Fact]
    public async Task GetEncryptedPayload_Returns_Envelope()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-3"), TestEncryptedPayload.Valid());

        var env = await repo.GetEncryptedPayloadAsync("tok-3");

        env.Should().NotBeNull();
        env.WrapPayload.Should().NotBeNull();
    }

    [Fact]
    public async Task IncrementUsage_ThrowsException_When_Token_Not_Found()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.IncrementUsageAsync("non-existent-token", now));
    }

    [Fact]
    public async Task IncrementUsage_ThrowsException_When_Token_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var now = DateTimeOffset.UtcNow;
        await Assert.ThrowsAsync<ArgumentException>(() => repo.IncrementUsageAsync("", now));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.IncrementUsageAsync(null!, now));
    }

    [Fact]
    public async Task IncrementUsage_Updates_Counters()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var args = TestCreateTokenArgs.Valid("tok-4") with { MaxUses = 1 };
        await repo.CreateAsync(args, TestEncryptedPayload.Valid());

        var now = DateTimeOffset.UtcNow;
        var result = await repo.IncrementUsageAsync("tok-4", now);

        result.UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task IncrementUsage_Updates_LastUsedAt()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("usage-test"), TestEncryptedPayload.Valid());

        var now = DateTimeOffset.UtcNow;
        var result = await repo.IncrementUsageAsync("usage-test", now);

        result.LastUsedAt.Should().Be(now);
        result.UsageCount.Should().Be(1);
    }

    [Fact]
    public async Task Deactivate_ReturnsFalse_When_Token_Not_Found()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.DeactivateAsync("non-existent-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_ThrowsException_When_Token_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeactivateAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeactivateAsync(null!));
    }

    [Fact]
    public async Task Deactivate_Is_Idempotent()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-5"), TestEncryptedPayload.Valid());

        var first = await repo.DeactivateAsync("tok-5");
        var second = await repo.DeactivateAsync("tok-5");

        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_Removes_Token_From_Database()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await repo.CreateAsync(TestCreateTokenArgs.Valid("tok-delete"), TestEncryptedPayload.Valid());

        var result = await repo.DeleteAsync("tok-delete");

        result.Should().BeTrue();

        var deleted = await repo.GetSummaryByTokenAsync("tok-delete");
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ReturnsFalse_When_Token_Not_Found()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.DeleteAsync("non-existent-token");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ThrowsException_When_Token_Is_Empty()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeleteAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(() => repo.DeleteAsync(null!));
    }

    [Fact]
    public async Task BulkCreate_Creates_Multiple_Tokens()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var tokenData = Enumerable.Range(1, 5)
            .Select(i => (TestCreateTokenArgs.Valid($"bulk-tok-{i}"), TestEncryptedPayload.Valid()))
            .ToList();

        var result = await repo.BulkCreateAsync(tokenData);

        result.Should().HaveCount(5);
        result.Select(t => t.Token).Should()
            .BeEquivalentTo(["bulk-tok-1", "bulk-tok-2", "bulk-tok-3", "bulk-tok-4", "bulk-tok-5"]);

        var count = await dbWrap.Context.Tokens.CountAsync(t => t.Token.StartsWith("bulk-tok-"));
        count.Should().Be(5);
    }

    [Fact]
    public async Task BulkCreate_ReturnsEmpty_When_No_Data()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.BulkCreateAsync([]);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BulkDeactivate_Deactivates_Multiple_Tokens()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        // Create tokens
        await repo.CreateAsync(TestCreateTokenArgs.Valid("bulk-deactivate-1"), TestEncryptedPayload.Valid());
        await repo.CreateAsync(TestCreateTokenArgs.Valid("bulk-deactivate-2"), TestEncryptedPayload.Valid());
        await repo.CreateAsync(TestCreateTokenArgs.Valid("bulk-deactivate-3"), TestEncryptedPayload.Valid());

        var tokens = new[] { "bulk-deactivate-1", "bulk-deactivate-2", "bulk-deactivate-3" };
        var result = await repo.BulkDeactivateAsync(tokens);

        result.Should().Be(3);

        // Verify all tokens are deactivated
        var deactivatedTokens = await dbWrap.Context.Tokens
            .AsNoTracking()
            .Where(t => t.Token.StartsWith("bulk-deactivate-") && !t.IsActive)
            .CountAsync();
        deactivatedTokens.Should().Be(3);
    }

    [Fact]
    public async Task BulkDeactivate_ReturnsZero_When_No_Tokens()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        var result = await repo.BulkDeactivateAsync([]);

        result.Should().Be(0);
    }

    [Fact]
    public async Task BulkDeactivate_Skips_Already_Deactivated_Tokens()
    {
        var dbWrap = await sqlFixture.CreateScopeAsync();
        var repo = new TokenRecordRepository(dbWrap.Context, dbWrap.Blind,
            new BulkOperationsService(dbWrap.Context, BulkLogger));

        // Create and deactivate one token manually
        await repo.CreateAsync(TestCreateTokenArgs.Valid("bulk-skip-1"), TestEncryptedPayload.Valid());
        await repo.DeactivateAsync("bulk-skip-1");

        // Create another active token
        await repo.CreateAsync(TestCreateTokenArgs.Valid("bulk-skip-2"), TestEncryptedPayload.Valid());

        var tokens = new[] { "bulk-skip-1", "bulk-skip-2" };
        var result = await repo.BulkDeactivateAsync(tokens);

        // Should only deactivate 1 token (the second one)
        result.Should().Be(1);
    }
}
