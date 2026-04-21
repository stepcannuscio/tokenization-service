using FluentAssertions;
using Tokenization.Domain.Abstractions;
using Moq;
using System.Text;
using Tokenization.Domain.Exceptions;
using Tokenization.Application.Services;
using Tokenization.Domain.ValueObjects;
using Tokenization.Tests.Shared.Utils.ValueObjects;
using Xunit;

namespace Tokenization.Tests.Unit.Application.Services;

public class TokenServiceTests
{
    private class TokenServiceWrap(
        Mock<ITokenRecordRepository> repo,
        Mock<IEncryptionService> crypto,
        Mock<ITenantContextService> tenantCtx,
        Mock<ILogger<TokenService>> logger)
    {
        public readonly Mock<ITokenRecordRepository> Repo = repo;
        public readonly Mock<IEncryptionService> Crypto = crypto;
        public readonly Mock<ITenantContextService> TenantCtx = tenantCtx;

        public readonly ITokenService Svc =
            new TokenService(repo.Object, crypto.Object, tenantCtx.Object, logger.Object);
    }

    private static TokenServiceWrap GetTokenServiceWrap()
    {
        var repo = new Mock<ITokenRecordRepository>(MockBehavior.Strict);
        var crypto = new Mock<IEncryptionService>(MockBehavior.Strict);
        var tenantCtx = new Mock<ITenantContextService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TokenService>>();

        return new TokenServiceWrap(repo, crypto, tenantCtx, logger);
    }
    
    [Fact]
    public async Task IssueToken_WithNullArgs_Throws()
    {
        var wrap = GetTokenServiceWrap();
        const string plaintext = "{ \"pan\":\"4111111111111111\" }";

        await FluentActions.Invoking(() => wrap.Svc.IssueTokenAsync(null!, Encoding.UTF8.GetBytes(plaintext)))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task IssueToken_WithEmptyPayload_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var args = TestCreateTokenArgs.Valid("tok-abc");

        await FluentActions.Invoking(() => wrap.Svc.IssueTokenAsync(args, ReadOnlyMemory<byte>.Empty))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Sensitive payload is required.*");
    }

    [Fact]
    public async Task IssueToken_Encrypts_And_Persists()
    {
        var wrap = GetTokenServiceWrap();

        var args = TestCreateTokenArgs.Valid("tok-abc");
        const string plaintext = "{ \"pan\":\"4111111111111111\" }";
        var env = TestEncryptedPayload.Valid();

        var summary = TestTokenSummary.Valid();

        wrap.Crypto.Setup(c => c.EncryptAsync(plaintext, It.IsAny<CancellationToken>()))
            .ReturnsAsync(env);
        wrap.Repo.Setup(r => r.CreateAsync(args, env, It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(args.TenantId)).Verifiable();

        var result = await wrap.Svc.IssueTokenAsync(args, Encoding.UTF8.GetBytes(plaintext));

        result.Token.Should().Be(summary.Token);
        wrap.Crypto.VerifyAll();
        wrap.Repo.VerifyAll();
        wrap.TenantCtx.VerifyAll();
    }

    [Fact]
    public async Task IssueToken_InvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var args = TestCreateTokenArgs.Valid("tok-abc");
        const string plaintext = "{ \"pan\":\"4111111111111111\" }";

        wrap.Crypto.Setup(c => c.EncryptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Verifiable();
        wrap.Repo.Setup(r =>
                r.CreateAsync(It.IsAny<CreateTokenArgs>(), It.IsAny<EncryptedPayload>(), It.IsAny<CancellationToken>()))
            .Verifiable();
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(It.IsAny<string>()))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.IssueTokenAsync(args, Encoding.UTF8.GetBytes(plaintext)))
            .Should().ThrowAsync<TenantAccessDeniedException>();
        wrap.Crypto.Verify(s => s.EncryptAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        wrap.Repo.Verify(
            s => s.CreateAsync(It.IsAny<CreateTokenArgs>(), It.IsAny<EncryptedPayload>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task IssueToken_WithCancellation_RespectsCancellationToken()
    {
        var wrap = GetTokenServiceWrap();
        var args = TestCreateTokenArgs.Valid("tok-abc");
        const string plaintext = "{ \"pan\":\"4111111111111111\" }";
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(args.TenantId))
            .Throws(new OperationCanceledException());

        await FluentActions.Invoking(() => wrap.Svc.IssueTokenAsync(args, Encoding.UTF8.GetBytes(plaintext), cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GetSummary_ExistentToken_Returns()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        var got = await wrap.Svc.GetSummaryAsync("tok");
        got.Should().Be(summary);
        wrap.TenantCtx.VerifyAll();
    }
    
    [Fact]
    public async Task GetSummary_NonExistentToken_Throws()
    {
        var wrap = GetTokenServiceWrap();
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("missing", CancellationToken.None))
            .ReturnsAsync((TokenSummary?)null);
        
        await FluentActions.Invoking(() => wrap.Svc.GetSummaryAsync("missing"))
            .Should().ThrowAsync<TokenNotFoundException>();
    }
        
    [Fact]
    public async Task GetSummary_InvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.GetSummaryAsync("tok", CancellationToken.None))
            .Should().ThrowAsync<TenantAccessDeniedException>();
    }
    
    [Fact]
    public async Task GetSummary_WithCancellation_RespectsCancellationToken()
    {
        var wrap = GetTokenServiceWrap();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", cts.Token))
            .Throws(new OperationCanceledException());

        await FluentActions.Invoking(() => wrap.Svc.GetSummaryAsync("tok", cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task FindByTenantCustomer_ValidRequest_ReturnsTokens()
    {
        var wrap = GetTokenServiceWrap();
        var tokens = new List<TokenSummary>
        {
            TestTokenSummary.Valid(),
            TestTokenSummary.Valid()
        };

        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess("tenant123")).Verifiable();
        wrap.Repo.Setup(r => r.FindByTenantCustomerAsync("tenant123", "customer456", 50, CancellationToken.None))
            .ReturnsAsync(tokens);

        var result = await wrap.Svc.FindByTenantCustomerAsync("tenant123", "customer456");

        result.Should().HaveCount(2);
        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }

    [Fact]
    public async Task FindByTenantCustomer_InvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();

        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess("tenant123"))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.FindByTenantCustomerAsync("tenant123", "customer456"))
            .Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task FindByTenantCustomer_WithTakeParameter_RespectsLimit()
    {
        var wrap = GetTokenServiceWrap();
        var tokens = new List<TokenSummary> { TestTokenSummary.Valid() };

        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess("tenant123")).Verifiable();
        wrap.Repo.Setup(r => r.FindByTenantCustomerAsync("tenant123", "customer456", 10, CancellationToken.None))
            .ReturnsAsync(tokens);

        var result = await wrap.Svc.FindByTenantCustomerAsync("tenant123", "customer456", 10);

        result.Should().HaveCount(1);
        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }

    [Fact]
    public async Task FindByTenantCustomer_WithTakeLessThanOne_EnforcesMinimum()
    {
        var wrap = GetTokenServiceWrap();
        var tokens = new List<TokenSummary> { TestTokenSummary.Valid() };

        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess("tenant123")).Verifiable();
        wrap.Repo.Setup(r => r.FindByTenantCustomerAsync("tenant123", "customer456", 1, CancellationToken.None))
            .ReturnsAsync(tokens);

        var result = await wrap.Svc.FindByTenantCustomerAsync("tenant123", "customer456", 0);

        result.Should().HaveCount(1);
        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }

    [Fact]
    public async Task RedeemToken_WithValidToken_ReturnsUpdatedSummary()
    {
        var wrap = GetTokenServiceWrap();
        var now = DateTimeOffset.UtcNow;
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(summary.Token, CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.IncrementUsageAsync(summary.Token, now, CancellationToken.None))
            .ReturnsAsync(new TokenUsageResult(Token: summary.Token, UsageCount: 1, MaxUses: 5, IsActive: true,
                LastUsedAt: now));
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        var result = await wrap.Svc.RedeemTokenAsync(summary.Token, now);

        result.UsageCount.Should().Be(1);
        result.LastUsedAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(2));
        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }

    [Fact]
    public async Task RedeemToken_WithNonExistentToken_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var now = DateTimeOffset.UtcNow;

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("missing", CancellationToken.None))
            .ReturnsAsync((TokenSummary?)null);

        await FluentActions.Invoking(() => wrap.Svc.RedeemTokenAsync("missing", now))
            .Should().ThrowAsync<TokenNotFoundException>();
    }

    [Fact]
    public async Task RedeemToken_WithInvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var now = DateTimeOffset.UtcNow;
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.RedeemTokenAsync("tok", now))
            .Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task RedeemToken_WhenUsageLimitReached_DeactivatesToken()
    {
        var wrap = GetTokenServiceWrap();
        var now = DateTimeOffset.UtcNow;
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(summary.Token, CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.IncrementUsageAsync(summary.Token, now, CancellationToken.None))
            .ReturnsAsync(new TokenUsageResult(Token: summary.Token, UsageCount: 5, MaxUses: 5, IsActive: true,
                LastUsedAt: now));
        wrap.Repo.Setup(r => r.DeactivateAsync(summary.Token, CancellationToken.None)).ReturnsAsync(true).Verifiable();
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        var result = await wrap.Svc.RedeemTokenAsync(summary.Token, now);

        result.IsActive.Should().BeFalse();
        result.UsageCount.Should().Be(5);
        wrap.Repo.VerifyAll();
        wrap.TenantCtx.VerifyAll();
    }

    [Fact]
    public async Task RedeemToken_Throws_When_Inactive_Expired_Or_Exceeded()
    {
        var wrap = GetTokenServiceWrap();
        var now = DateTimeOffset.UtcNow;

        // Inactive
        var inactive = TestTokenSummary.Valid() with
        {
            Token = "inactive", IsActive = false
        };
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(inactive.Token, CancellationToken.None)).ReturnsAsync(inactive);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(inactive.TenantId)).Verifiable();
        await FluentActions.Invoking(() => wrap.Svc.RedeemTokenAsync(inactive.Token, now))
            .Should().ThrowAsync<TokenInactiveException>();
        wrap.TenantCtx.VerifyAll();

        // Expired
        var expired = TestTokenSummary.Valid() with
        {
            Token = "expired", ExpiresAt = now.AddMinutes(-1)
        };
        wrap.Repo.Reset();
        wrap.TenantCtx.Reset();
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(expired.Token, CancellationToken.None)).ReturnsAsync(expired);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(expired.TenantId)).Verifiable();
        await FluentActions.Invoking(() => wrap.Svc.RedeemTokenAsync(expired.Token, now))
            .Should().ThrowAsync<TokenExpiredException>();
        wrap.TenantCtx.VerifyAll();

        // Exceeded
        wrap.Repo.Reset();
        wrap.TenantCtx.Reset();
        var exceeded = TestTokenSummary.Valid() with
        {
            Token = "exceeded", UsageCount = 5, MaxUses = 5
        };
        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(exceeded.Token, CancellationToken.None)).ReturnsAsync(exceeded);
        wrap.Repo.Setup(r => r.DeactivateAsync(exceeded.Token, CancellationToken.None)).ReturnsAsync(true).Verifiable();
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(exceeded.TenantId)).Verifiable();
        await FluentActions.Invoking(() => wrap.Svc.RedeemTokenAsync(exceeded.Token, now))
            .Should().ThrowAsync<TokenUsageExceededException>();
        wrap.TenantCtx.VerifyAll();
    }
    
    [Fact]
    public async Task DetokenizeToken_WithValidToken_ReturnsDecryptedPayload()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();
        var env = TestEncryptedPayload.Valid();
        const string decryptedPayload = "{\"pan\":\"4111111111111111\"}";

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(summary.Token, CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.GetEncryptedPayloadAsync(summary.Token, CancellationToken.None)).ReturnsAsync(env);
        wrap.Crypto.Setup(c => c.DecryptAsync(env, CancellationToken.None)).ReturnsAsync(decryptedPayload);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        var result = await wrap.Svc.DetokenizeTokenAsync(summary.Token);

        result.Plaintext.Should().Be(decryptedPayload);
        result.TokenSummary.Should().Be(summary);
        wrap.Repo.VerifyAll();
        wrap.Crypto.VerifyAll();
        wrap.TenantCtx.VerifyAll();
    }

    [Fact]
    public async Task DetokenizeToken_WithNonExistentToken_Throws()
    {
        var wrap = GetTokenServiceWrap();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("missing", CancellationToken.None))
            .ReturnsAsync((TokenSummary?)null);

        await FluentActions.Invoking(() => wrap.Svc.DetokenizeTokenAsync("missing"))
            .Should().ThrowAsync<TokenNotFoundException>();
    }

    [Fact]
    public async Task DetokenizeToken_WithInvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(summary.Token, CancellationToken.None)).ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.DetokenizeTokenAsync(summary.Token))
            .Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task DetokenizeToken_WithMissingEncryptedPayload_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync(summary.Token, CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.GetEncryptedPayloadAsync(summary.Token, CancellationToken.None))
            .ReturnsAsync((EncryptedPayload?)null);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        await FluentActions.Invoking(() => wrap.Svc.DetokenizeTokenAsync(summary.Token))
            .Should().ThrowAsync<TokenNotFoundException>();

        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }

    [Fact]
    public async Task DeleteToken_Throws_When_NotFound()
    {
        var wrap = GetTokenServiceWrap();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("missing", CancellationToken.None))
            .ReturnsAsync((TokenSummary?)null);

        await FluentActions.Invoking(() => wrap.Svc.DeleteTokenAsync("missing"))
            .Should().ThrowAsync<TokenNotFoundException>();
    }

    [Fact]
    public async Task DeleteToken_WithValidToken_DeletesSuccessfully()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.DeleteAsync("tok", CancellationToken.None)).ReturnsAsync(true);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        await wrap.Svc.DeleteTokenAsync("tok");

        wrap.Repo.VerifyAll();
        wrap.TenantCtx.VerifyAll();
    }

    [Fact]
    public async Task DeleteToken_WithNonExistentToken_Throws()
    {
        var wrap = GetTokenServiceWrap();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("missing", CancellationToken.None))
            .ReturnsAsync((TokenSummary?)null);

        await FluentActions.Invoking(() => wrap.Svc.DeleteTokenAsync("missing"))
            .Should().ThrowAsync<TokenNotFoundException>();
    }

    [Fact]
    public async Task DeleteToken_WithInvalidTenantAccess_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId))
            .Throws(new TenantAccessDeniedException("123"));

        await FluentActions.Invoking(() => wrap.Svc.DeleteTokenAsync("tok"))
            .Should().ThrowAsync<TenantAccessDeniedException>();
    }

    [Fact]
    public async Task DeleteToken_WhenRepositoryReturnsFalse_Throws()
    {
        var wrap = GetTokenServiceWrap();
        var summary = TestTokenSummary.Valid();

        wrap.Repo.Setup(r => r.GetSummaryByTokenAsync("tok", CancellationToken.None)).ReturnsAsync(summary);
        wrap.Repo.Setup(r => r.DeleteAsync("tok", CancellationToken.None)).ReturnsAsync(false);
        wrap.TenantCtx.Setup(r => r.ValidateTenantAccess(summary.TenantId)).Verifiable();

        await FluentActions.Invoking(() => wrap.Svc.DeleteTokenAsync("tok"))
            .Should().ThrowAsync<TokenNotFoundException>();

        wrap.TenantCtx.VerifyAll();
        wrap.Repo.VerifyAll();
    }
}
