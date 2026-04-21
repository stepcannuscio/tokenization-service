using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

namespace Tokenization.Api.Versioning;

/// <summary>
/// API versioning policies and conventions for the Tokenization API.
/// </summary>
internal static class ApiVersioningPolicies
{
    /// <summary>
    /// Gets the API versioning options configured for enterprise best practices.
    /// </summary>
    /// <returns>Configured API versioning options.</returns>
    public static ApiVersioningOptions GetVersioningOptions()
    {
        return new ApiVersioningOptions
        {
            // Primary versioning strategy: URL path versioning
            ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader(VersioningParams.Header),
                new QueryStringApiVersionReader(VersioningParams.Query)
            ),

            // Assume version 1.0 if no version is specified
            AssumeDefaultVersionWhenUnspecified = true,
            DefaultApiVersion = new ApiVersion(1, 0),

            // Report API versions in response headers
            ReportApiVersions = true
        };
    }
}
