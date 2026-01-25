using PayBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayBridge.Domain.ValueObjects;

/// <summary>
/// Represents a unique payment reference
/// Format: PB_[32 hex characters]
/// </summary>
public sealed class PaymentReference : ValueObject
{
    public string Value { get; }

    private const string Prefix = "PB_";
    private static readonly Regex ValidationRegex = new(
        @"^PB_[a-f0-9]{32}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private PaymentReference(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Generates a new unique payment reference
    /// </summary>
    public static PaymentReference Generate()
    {
        var reference = $"{Prefix}{Guid.NewGuid():N}";
        return new PaymentReference(reference);
    }

    /// <summary>
    /// Creates a PaymentReference from an existing string with validation
    /// Throws InvalidPaymentReferenceException if invalid
    /// </summary>
    public static PaymentReference Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidPaymentReferenceException(
                "Payment reference cannot be empty",
                "EMPTY_REFERENCE");

        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidPaymentReferenceException(
                $"Payment reference must start with '{Prefix}'",
                "INVALID_PREFIX");

        if (!ValidationRegex.IsMatch(value))
            throw new InvalidPaymentReferenceException(
                "Payment reference format is invalid",
                "INVALID_FORMAT");

        return new PaymentReference(value);
    }

    public override string ToString() => Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
