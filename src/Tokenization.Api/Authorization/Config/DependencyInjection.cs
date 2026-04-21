namespace Tokenization.Api.Authorization.Config;

/// <summary>
/// DI registration for authorization.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Configures authorization services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddTokenizationAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanReadTokens, p =>
                p.RequireAuthenticatedUser().RequireAssertion(ctx => AuthorizationHandler.IsAuthorized(
                    ctx.User,
                    Scopes.TokenRead,
                    Roles.TokenAdmin)))
            .AddPolicy(Policies.CanCreateTokens, p =>
                p.RequireAuthenticatedUser().RequireAssertion(ctx => AuthorizationHandler.IsAuthorized(
                    ctx.User,
                    Scopes.TokenCreate,
                    Roles.TokenAdmin)))
            .AddPolicy(Policies.CanDeleteTokens, p =>
                p.RequireAuthenticatedUser().RequireAssertion(ctx => AuthorizationHandler.IsAuthorized(
                    ctx.User,
                    Scopes.TokenDelete,
                    Roles.TokenAdmin)))
            .AddPolicy(Policies.CanDetokenizeTokens, p =>
                p.RequireAuthenticatedUser().RequireAssertion(ctx => AuthorizationHandler.IsAuthorized(
                    ctx.User,
                    Scopes.TokenDetokenize,
                    Roles.TokenAdmin)));
    }
}
