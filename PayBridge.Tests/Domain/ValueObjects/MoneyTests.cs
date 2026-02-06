using FluentAssertions;
using PayBridge.Domain.Exceptions;
using PayBridge.Domain.ValueObjects;

namespace PayBridge.Tests.Domain.ValueObjects
{
    public class MoneyTests
    {

        #region Create Tests


        [Fact]
        public void Create_WithValidAmount_ShouldSucceed()
        {
            //Arrange
            var amount = 100.50m;
            var currency = "NGN";

            //Act 
            var money = Money.Create(amount, currency);

            //Assert
            money.Amount.Should().Be(100.5m);
            money.Currency.Should().Be("NGN");

        }

        [Fact]
        public void Create_WithZeroAmount_ShouldThrowInvalidMoneyException()
        {
            // Arrange
            var amount = 0m;

            // Act
            var act = () => Money.Create(amount);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Amount must be greater than zero")
                .And.ErrorCode.Should().Be("AMOUNT_NOT_POSITIVE");
        }
        [Fact]

        public void Create_WithNegativeAmount_ShouldThrowInvalidMoneyException()
        {
            //Arrange
            var amount = -50m;

            //Act
            var act = () => Money.Create(amount);

            //Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Amount must be greater than zero");
        }

        [Theory]
        [InlineData(100_000_001)]
        [InlineData(999_999_999)]
        public void Create_WithAmountExceedingMaximum_ShouldThrowInvalidMoneyException(decimal amount)
        {
            // Act
            var act = () => Money.Create(amount);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Amount exceeds maximum allowed value")
                .And.ErrorCode.Should().Be("AMOUNT_TOO_LARGE");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullCurrency_ShouldThrowInvalidMoneyException(string currency)
        {
            // Act
            var act = () => Money.Create(100m, currency);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Currency is required")
                .And.ErrorCode.Should().Be("CURRENCY_REQUIRED");
        }

        [Theory]
        [InlineData("XYZ")]
        [InlineData("INVALID")]
        [InlineData("123")]
        public void Create_WithUnsupportedCurrency_ShouldThrowInvalidMoneyException(string currency)
        {
            // Act
            var act = () => Money.Create(100m, currency);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .And.ErrorCode.Should().Be("UNSUPPORTED_CURRENCY");
        }

        [Theory]
        [InlineData("ngn", "NGN")]
        [InlineData("usd", "USD")]
        [InlineData("Gbp", "GBP")]
        [InlineData(" EUR ", "EUR")]
        public void Create_ShouldNormalizeCurrencyToUpperCase(string input, string expected)
        {
            // Act
            var money = Money.Create(100m, input);

            // Assert
            money.Currency.Should().Be(expected);
        }

        [Theory]
        [InlineData(100.123, 100.12)]
        [InlineData(100.125, 100.13)]
        [InlineData(100.999, 101.00)]
        public void Create_ShouldRoundAmountToTwoDecimalPlaces(decimal input, decimal expected)
        {
            // Act
            var money = Money.Create(input);

            // Assert
            money.Amount.Should().Be(expected);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void TwoMoneyObjects_WithSameValues_ShouldBeEqual()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(100m, "NGN");

            // Assert
            money1.Should().Be(money2);
            (money1 == money2).Should().BeTrue();
        }

        [Fact]
        public void TwoMoneyObjects_WithDifferentAmounts_ShouldNotBeEqual()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(200m, "NGN");

            // Assert
            money1.Should().NotBe(money2);
            (money1 != money2).Should().BeTrue();
        }

        [Fact]
        public void TwoMoneyObjects_WithDifferentCurrencies_ShouldNotBeEqual()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(100m, "USD");

            // Assert
            money1.Should().NotBe(money2);
        }
        #endregion


        #region Conversion Tests

        [Fact]
        public void ToKobo_ShouldConvertNGNCorrectly()
        {
            // Arrange
            var money = Money.Create(100.50m, "NGN");

            // Act
            var kobo = money.ToKobo();

            // Assert
            kobo.Should().Be(10050);
        }

        [Fact]
        public void ToKobo_WithNonNGNCurrency_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var money = Money.Create(100m, "USD");

            // Act
            var act = () => money.ToKobo();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("ToKobo only valid for NGN currency");
        }

        [Theory]
        [InlineData("USD")]
        [InlineData("GBP")]
        [InlineData("EUR")]
        public void ToCents_WithValidCurrency_ShouldConvertCorrectly(string currency)
        {
            // Arrange
            var money = Money.Create(100.50m, currency);

            // Act
            var cents = money.ToCents();

            // Assert
            cents.Should().Be(10050);
        }


        [Fact]
        public void ToCents_WithNGNCurrency_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var money = Money.Create(100m, "NGN");

            // Act
            var act = () => money.ToCents();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }
        #endregion

        #region Arithmetic Operations Tests

        [Fact]
        public void Add_WithSameCurrency_ShouldReturnCorrectSum()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(50m, "NGN");

            // Act
            var result = money1.Add(money2);

            // Assert
            result.Amount.Should().Be(150m);
            result.Currency.Should().Be("NGN");
        }

        [Fact]
        public void Add_WithDifferentCurrencies_ShouldThrowInvalidMoneyException()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(50m, "USD");

            // Act
            var act = () => money1.Add(money2);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Cannot add money with different currencies")
                .And.ErrorCode.Should().Be("CURRENCY_MISMATCH");
        }

        [Fact]
        public void Subtract_WithSameCurrency_ShouldReturnCorrectDifference()
        {
            // Arrange
            var money1 = Money.Create(100m, "NGN");
            var money2 = Money.Create(30m, "NGN");

            // Act
            var result = money1.Subtract(money2);

            // Assert
            result.Amount.Should().Be(70m);
            result.Currency.Should().Be("NGN");
        }

        [Fact]
        public void Subtract_ResultingInZeroOrNegative_ShouldThrowInvalidMoneyException()
        {
            // Arrange
            var money1 = Money.Create(50m, "NGN");
            var money2 = Money.Create(50m, "NGN");

            // Act
            var act = () => money1.Subtract(money2);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Cannot subtract to zero or negative amount")
                .And.ErrorCode.Should().Be("NEGATIVE_RESULT");
        }

        [Fact]
        public void MultiplyBy_WithPositiveFactor_ShouldReturnCorrectProduct()
        {
            // Arrange
            var money = Money.Create(100m, "NGN");

            // Act
            var result = money.MultiplyBy(2.5m);

            // Assert
            result.Amount.Should().Be(250m);
            result.Currency.Should().Be("NGN");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-5.5)]
        public void MultiplyBy_WithZeroOrNegativeFactor_ShouldThrowInvalidMoneyException(decimal factor)
        {
            // Arrange
            var money = Money.Create(100m, "NGN");

            // Act
            var act = () => money.MultiplyBy(factor);

            // Assert
            act.Should().Throw<InvalidMoneyException>()
                .WithMessage("Cannot multiply by zero or negative factor")
                .And.ErrorCode.Should().Be("INVALID_FACTOR");
        }

        #endregion

        #region Comparison Tests
        [Fact]
        public void GreaterThan_Operator_ShouldWorkCorrectly()
        {
            // Arrange
            var larger = Money.Create(100m, "NGN");
            var smaller = Money.Create(50m, "NGN");

            // Assert
            (larger > smaller).Should().BeTrue();
            (smaller > larger).Should().BeFalse();
        }

        [Fact]
        public void LessThan_Operator_ShouldWorkCorrectly()
        {
            // Arrange
            var larger = Money.Create(100m, "NGN");
            var smaller = Money.Create(50m, "NGN");

            // Assert
            (smaller < larger).Should().BeTrue();
            (larger < smaller).Should().BeFalse();
        }

        [Fact]
        public void ComparisonOperators_WithDifferentCurrencies_ShouldThrowInvalidMoneyException()
        {
            // Arrange
            var ngn = Money.Create(100m, "NGN");
            var usd = Money.Create(100m, "USD");

            // Act & Assert
            var act1 = () => ngn > usd;
            var act2 = () => ngn < usd;

            act1.Should().Throw<InvalidMoneyException>()
                .WithMessage("Cannot compare money with different currencies");
            act2.Should().Throw<InvalidMoneyException>()
                .WithMessage("Cannot compare money with different currencies");
        }

        #endregion

        #region ToString Tests

        [Theory]
        [InlineData("NGN", 100.50, "₦100.50")]
        [InlineData("USD", 100.50, "$100.50")]
        [InlineData("GBP", 100.50, "£100.50")]
        [InlineData("EUR", 100.50, "€100.50")]
        public void ToString_ShouldFormatCorrectlyForDifferentCurrencies(
            string currency, decimal amount, string expected)
        {
            // Arrange
            var money = Money.Create(amount, currency);

            // Act
            var result = money.ToString();

            // Assert
            result.Should().Be(expected);
        }

        #endregion
    }
}
