using PayBridge.Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Domain.ValueObjects
{
    /// <summary>
    /// Represents monetary value with currency.
    /// Throws domain exceptions for invalid states.
    /// Mordern .NET could declare this as a record, making it much lighter
    /// </summary>
    public sealed class Money : ValueObject
    {
        public decimal Amount { get;}
        public string Currency { get;}

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        /// <summary>
        /// Creates a Money instance with validation
        /// Throws InvalidMoneyException if invalid
        /// </summary>
        public static Money Create(decimal amount, string currency = "NGN")
        {
            var roundedAmount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

            if (roundedAmount <= 0)
                throw new InvalidMoneyException(
                    "Amount must be greater than zero",
                    "AMOUNT_NOT_POSITIVE");

            if (roundedAmount > 100_000_000m)
                throw new InvalidMoneyException(
                    "Amount exceeds maximum allowed value",
                    "AMOUNT_TOO_LARGE");

            if (string.IsNullOrWhiteSpace(currency))
                throw new InvalidMoneyException(
                    "Currency is required",
                    "CURRENCY_REQUIRED");

            var normalizedCurrency = currency.Trim().ToUpperInvariant();

            if (!IsValidCurrency(normalizedCurrency))
                throw new InvalidMoneyException(
                    $"Currency '{currency}' is not supported",
                    "UNSUPPORTED_CURRENCY");

            return new Money(roundedAmount, normalizedCurrency);
        }

        /// <summary>
        /// Converts to smallest currency unit (e.g., kobo, cents)
        /// </summary>
        public long ToMinorUnit(int decimals = 2)
        {
            var multiplier = (decimal)Math.Pow(10, decimals);
            var result = Amount * multiplier;

            if (result > long.MaxValue)
                throw new InvalidMoneyException(
                    "Amount too large to convert to minor units",
                    "CONVERSION_OVERFLOW");

            return (long)result;
        }

        /// <summary>
        /// Converts to kobo (Nigerian currency minor unit)
        /// </summary>
        public long ToKobo()
        {
            if (Currency != "NGN")
                throw new InvalidOperationException("ToKobo only valid for NGN currency");

            return ToMinorUnit(2);
        }

        /// <summary>
        /// Converts to cents (USD/GBP/EUR minor unit)
        /// </summary>
        public long ToCents()
        {
            if (!new[] { "USD", "GBP", "EUR" }.Contains(Currency))
                throw new InvalidOperationException("ToCents only valid for USD, GBP, or EUR");

            return ToMinorUnit(2);
        }

        /// <summary>
        /// Adds two money values (must be same currency)
        /// </summary>
        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidMoneyException(
                    "Cannot add money with different currencies",
                    "CURRENCY_MISMATCH");

            return Create(Amount + other.Amount, Currency);
        }

        /// <summary>
        /// Subtracts two money values (must be same currency)
        /// </summary>
        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidMoneyException(
                    "Cannot subtract money with different currencies",
                    "CURRENCY_MISMATCH");

            var result = Amount - other.Amount;

            if (result <= 0)
                throw new InvalidMoneyException(
                    "Cannot subtract to zero or negative amount",
                    "NEGATIVE_RESULT");

            return Create(result, Currency);
        }

        /// <summary>
        /// Multiplies money by a factor
        /// </summary>
        public Money MultiplyBy(decimal factor)
        {
            if (factor <= 0)
                throw new InvalidMoneyException(
                    "Cannot multiply by zero or negative factor",
                    "INVALID_FACTOR");

            return Create(Amount * factor, Currency);
        }

        public override string ToString()
        {
            return Currency switch
            {
                "NGN" => $"₦{Amount:N2}",
                "USD" => $"${Amount:N2}",
                "GBP" => $"£{Amount:N2}",
                "EUR" => $"€{Amount:N2}",
                _ => $"{Currency} {Amount:N2}"
            };
        }


        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }

        private static bool IsValidCurrency(string currency)
        {
            var supportedCurrencies = new[] { "NGN", "USD", "GBP", "EUR" };
            return supportedCurrencies.Contains(currency);
        }

        public static bool operator >(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidMoneyException(
                    "Cannot compare money with different currencies",
                    "CURRENCY_MISMATCH");
            return left.Amount > right.Amount;
        }

        public static bool operator <(Money left, Money right)
        {
            if (left.Currency != right.Currency)
                throw new InvalidMoneyException(
                    "Cannot compare money with different currencies",
                    "CURRENCY_MISMATCH");
            return left.Amount < right.Amount;
        }

        public static bool operator >=(Money left, Money right) => left > right || left == right;
        public static bool operator <=(Money left, Money right) => left < right || left == right;
    }
}

