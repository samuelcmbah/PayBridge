using PayBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.ValueObjects
{
    /// <summary>
    /// Represents a valid HTTP/HTTPS URL
    /// </summary>
    public sealed class Url : ValueObject
    {
        public string Value { get; }

        private Url(string value)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a Url with validation
        /// Throws InvalidUrlException if invalid
        /// </summary>
        public static Url Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidUrlException(
                    "URL cannot be empty",
                    "EMPTY_URL");

            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
                throw new InvalidUrlException(
                    "URL format is invalid",
                    "INVALID_URL_FORMAT");

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                throw new InvalidUrlException(
                    "URL must use HTTP or HTTPS scheme",
                    "INVALID_URL_SCHEME");

            return new Url(value);
        }

        public override string ToString() => Value;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
