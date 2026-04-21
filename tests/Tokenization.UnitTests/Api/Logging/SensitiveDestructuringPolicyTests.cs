using System.Reflection;
using FluentAssertions;
using Serilog.Core;
using Serilog.Events;
using Tokenization.Api.Logging;
using Xunit;

namespace Tokenization.Tests.Unit.Api.Logging;

/// <summary>
/// Unit tests for the SensitiveDestructuringPolicy class to ensure proper handling of sensitive data.
/// </summary>
public class SensitiveDestructuringPolicyTests
{
    private readonly SensitiveDestructuringPolicy _policy = new();
    private readonly MockLogEventPropertyValueFactory _factory = new();

    [Fact]
    public void TryDestructure_WithPrimitiveTypes_ShouldReturnFalse()
    {
        // Arrange
        var testCases = new object[]
        {
            42,
            3.14,
            true,
            'A',
            (byte)255,
            (long)123456789,
            1.23f,
            99.99m,
            "test string",
            Guid.NewGuid(),
            DateTimeOffset.Now
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = _policy.TryDestructure(testCase, _factory, out var output);

            // Assert
            result.Should().BeFalse($"primitive type {testCase.GetType().Name} should not be destructured");
            output.Should().BeNull();
        }
    }

    [Fact]
    public void TryDestructure_WithObjectWithoutSensitiveProperties_ShouldReturnTrue()
    {
        // Arrange
        var testObject = new TestObject
        {
            Name = "John Doe",
            Age = 30,
            IsActive = true
        };

        // Act
        var result = _policy.TryDestructure(testObject, _factory, out var output);

        // Assert
        result.Should().BeTrue();
        output.Should().NotBeNull();
        output.Should().BeOfType<StructureValue>();

        var structure = output as StructureValue;
        structure!.TypeTag.Should().Be(nameof(TestObject));
        structure.Properties.Should().HaveCount(3);

        var properties = structure.Properties.ToDictionary(p => p.Name, p => p.Value);
        properties["Name"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be("John Doe");
        properties["Age"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(30);
        properties["IsActive"].Should().BeOfType<ScalarValue>().Which.Value.Should().Be(true);
    }

    [Theory]
    [InlineData(Sensitivity.Payment, "4111111111111111", "**** **** **** 1111")]
    [InlineData(Sensitivity.Payment, "5555555555554444", "**** **** **** 4444")]
    [InlineData(Sensitivity.Payment, "378282246310005", "**** **** **** 0005")]
    [InlineData(Sensitivity.Credential, "secretpassword", "********")]
    [InlineData(Sensitivity.Secret, "anysecret", "***redacted***")]
    [InlineData(Sensitivity.Pii, "personaldata", "***redacted***")]
    internal void TryDestructure_WithSensitiveProperties_ShouldMaskValues(Sensitivity sensitivity, string originalValue, string expectedMaskedValue)
    {
        // Arrange
        var testObject = CreateTestObjectWithSensitiveProperty(sensitivity, originalValue);

        // Act
        var result = _policy.TryDestructure(testObject, _factory, out var output);

        // Assert
        result.Should().BeTrue();
        output.Should().NotBeNull();

        var structure = output as StructureValue;
        var sensitiveProperty = structure!.Properties.FirstOrDefault(p => p.Name == "SensitiveData");
        sensitiveProperty.Should().NotBeNull();
        sensitiveProperty.Value.Should().BeOfType<ScalarValue>();
        sensitiveProperty.Value.As<ScalarValue>().Value.Should().Be(expectedMaskedValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData(null)]
    public void TryDestructure_WithShortOrNullPaymentData_ShouldReturnRedacted(string? value)
    {
        // Arrange
        var testObject = CreateTestObjectWithSensitiveProperty(Sensitivity.Payment, value);

        // Act
        var result = _policy.TryDestructure(testObject, _factory, out var output);

        // Assert
        result.Should().BeTrue();
        output.Should().NotBeNull();

        var structure = output as StructureValue;
        var sensitiveProperty = structure!.Properties.FirstOrDefault(p => p.Name == "SensitiveData");
        sensitiveProperty.Should().NotBeNull();
        sensitiveProperty.Value.Should().BeOfType<ScalarValue>();
        sensitiveProperty.Value.As<ScalarValue>().Value.Should().Be("***redacted***");
    }

    [Fact]
    public void TryDestructure_WithMixedProperties_ShouldHandleCorrectly()
    {
        // Arrange
        var testObject = new MixedSensitiveObject
        {
            PublicName = "John Doe",
            PaymentCard = "4111111111111111",
            Password = "secret123",
            Age = 25
        };

        // Act
        var result = _policy.TryDestructure(testObject, _factory, out var output);

        // Assert
        result.Should().BeTrue();
        output.Should().NotBeNull();

        var structure = output as StructureValue;
        var properties = structure!.Properties.ToDictionary(p => p.Name, p => p.Value);

        properties["PublicName"].As<ScalarValue>().Value.Should().Be("John Doe");
        properties["PaymentCard"].As<ScalarValue>().Value.Should().Be("**** **** **** 1111");
        properties["Password"].As<ScalarValue>().Value.Should().Be("********");
        properties["Age"].As<ScalarValue>().Value.Should().Be(25);
    }

    [Fact]
    public void TryDestructure_WithNullValue_ShouldHandleGracefully()
    {
        // Arrange
        object? nullObject = null;

        // Act
        var success = _policy.TryDestructure(nullObject!, _factory, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    private static object CreateTestObjectWithSensitiveProperty(Sensitivity sensitivity, string? value)
    {
        return sensitivity switch
        {
            Sensitivity.Payment => new TestObjectWithSensitiveProperty { SensitiveData = value },
            Sensitivity.Credential => new TestObjectWithCredentialProperty { SensitiveData = value },
            Sensitivity.Secret => new TestObjectWithSecretProperty { SensitiveData = value },
            Sensitivity.Pii => new TestObjectWithPiiProperty { SensitiveData = value },
            _ => new TestObjectWithSecretProperty { SensitiveData = value }
        };
    }

    // Test helper classes
    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }

    private class TestObjectWithSensitiveProperty
    {
        [Sensitive(Sensitivity.Payment)]
        public string? SensitiveData { get; set; }
    }

    private class TestObjectWithCredentialProperty
    {
        [Sensitive(Sensitivity.Credential)]
        public string? SensitiveData { get; set; }
    }

    private class TestObjectWithSecretProperty
    {
        [Sensitive(Sensitivity.Secret)]
        public string? SensitiveData { get; set; }
    }

    private class TestObjectWithPiiProperty
    {
        [Sensitive(Sensitivity.Pii)]
        public string? SensitiveData { get; set; }
    }

    private class MixedSensitiveObject
    {
        public string PublicName { get; set; } = string.Empty;

        [Sensitive(Sensitivity.Payment)]
        public string PaymentCard { get; set; } = string.Empty;

        [Sensitive(Sensitivity.Credential)]
        public string Password { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    private class ObjectWithNonReadableProperty
    {
        public string ReadableProperty { get; set; } = string.Empty;

        public string NonReadableProperty { private get; set; } = string.Empty;
    }

    // Mock factory for testing
    private class MockLogEventPropertyValueFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
        {
            return new ScalarValue(value);
        }
    }
}
