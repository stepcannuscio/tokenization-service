using FluentAssertions;
using Tokenization.Api.Logging;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Logging;

/// <summary>
/// Unit tests for the SensitiveAttribute class to ensure proper attribute behavior.
/// </summary>
public class SensitiveAttributeTests
{
    [Theory]
    [InlineData(Sensitivity.Pii)]
    [InlineData(Sensitivity.Secret)]
    [InlineData(Sensitivity.Payment)]
    [InlineData(Sensitivity.Credential)]
    internal void Constructor_WithSensitivityKind_ShouldSetKindProperty(Sensitivity sensitivityKind)
    {
        // Act
        var attribute = new SensitiveAttribute(sensitivityKind);

        // Assert
        attribute.Kind.Should().Be(sensitivityKind);
    }

    [Fact]
    public void Constructor_WithoutParameters_ShouldDefaultToSecret()
    {
        // Act
        var attribute = new SensitiveAttribute();

        // Assert
        attribute.Kind.Should().Be(Sensitivity.Secret);
    }

    [Fact]
    public void Attribute_ShouldAllowMultipleTargets()
    {
        // Arrange & Act
        var attribute = new SensitiveAttribute(Sensitivity.Payment);
        var usageAttribute = attribute.GetType().GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        usageAttribute.Should().NotBeNull();
        usageAttribute.ValidOn.Should().HaveFlag(AttributeTargets.Property);
        usageAttribute.ValidOn.Should().HaveFlag(AttributeTargets.Field);
    }

    [Fact]
    public void Attribute_ShouldNotAllowMultiple()
    {
        // Arrange & Act
        var attribute = new SensitiveAttribute(Sensitivity.Payment);
        var usageAttribute = attribute.GetType().GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        usageAttribute.Should().NotBeNull();
        usageAttribute.AllowMultiple.Should().BeFalse();
    }

    [Fact]
    public void Attribute_ShouldNotBeInherited()
    {
        // Arrange & Act
        var attribute = new SensitiveAttribute(Sensitivity.Payment);
        var usageAttribute = attribute.GetType().GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .FirstOrDefault() as AttributeUsageAttribute;

        // Assert
        usageAttribute.Should().NotBeNull();
        usageAttribute.Inherited.Should().BeFalse();
    }
}