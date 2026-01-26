using PayBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PayBridge.Domain.ValueObjects
{
    /// <summary>
    /// Represents a valid email address
    /// </summary>
    public sealed class Email : ValueObject
    {
        public string Value { get; }

        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Email(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates an Email with validation
        /// Throws InvalidEmailException if invalid
        /// </summary>
        public static Email Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidEmailException(
                    "Email address cannot be empty",
                    "EMPTY_EMAIL");

            var normalizedEmail = value.Trim().ToLowerInvariant();

            if (!EmailRegex.IsMatch(normalizedEmail))
                throw new InvalidEmailException(
                    "Email address format is invalid",
                    "INVALID_EMAIL_FORMAT");

            if (normalizedEmail.Length > 254)
                throw new InvalidEmailException(
                    "Email address is too long",
                    "EMAIL_TOO_LONG");

            return new Email(normalizedEmail);
        }

        public override string ToString() => Value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
