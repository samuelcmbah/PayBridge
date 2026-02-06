using FluentAssertions;
using PayBridge.Domain.Exceptions;
using PayBridge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PayBridge.Tests.Domain.ValueObjects
{
    public class EmailTests
    {
        #region Create Tests

        [Theory]
        [InlineData("user@example.com")]
        [InlineData("test.user@company.co.uk")]
        [InlineData("first+last@domain.org")]
        [InlineData("admin@sub.domain.com")]
        public void Create_WithValidEmail_ShouldSucceed(string email)
        {
            // Act
            var result = Email.Create(email);

            // Assert
            result.Value.Should().Be(email.Trim().ToLowerInvariant());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_WithEmptyOrNullEmail_ShouldThrowInvalidEmailException(string email)
        {
            // Act
            var act = () => Email.Create(email);

            // Assert
            act.Should().Throw<InvalidEmailException>()
                .WithMessage("Email address cannot be empty")
                .And.ErrorCode.Should().Be("EMPTY_EMAIL");
        }

        [Theory]
        [InlineData("notanemail")]
        [InlineData("@example.com")]
        [InlineData("user@")]
        [InlineData("user @example.com")]
        [InlineData("user@.com")]
        [InlineData("user@domain")]
        public void Create_WithInvalidFormat_ShouldThrowInvalidEmailException(string email)
        {
            // Act
            var act = () => Email.Create(email);

            // Assert
            act.Should().Throw<InvalidEmailException>()
                .WithMessage("Email address format is invalid")
                .And.ErrorCode.Should().Be("INVALID_EMAIL_FORMAT");
        }

        [Fact]
        public void Create_WithEmailExceeding254Characters_ShouldThrowInvalidEmailException()
        {
            // Arrange - Create an email longer than 254 characters
            var longEmail = new string('a', 250) + "@test.com";

            // Act
            var act = () => Email.Create(longEmail);

            // Assert
            act.Should().Throw<InvalidEmailException>()
                .WithMessage("Email address is too long")
                .And.ErrorCode.Should().Be("EMAIL_TOO_LONG");
        }

        [Theory]
        [InlineData("User@Example.COM", "user@example.com")]
        [InlineData("ADMIN@DOMAIN.COM", "admin@domain.com")]
        [InlineData("  Test@Email.Com  ", "test@email.com")]
        public void Create_ShouldNormalizeEmailToLowerCase(string input, string expected)
        {
            // Act
            var email = Email.Create(input);

            // Assert
            email.Value.Should().Be(expected);
        }

        #endregion

        #region Equality Tests

        [Fact]
        public void TwoEmailObjects_WithSameValue_ShouldBeEqual()
        {
            // Arrange
            var email1 = Email.Create("user@example.com");
            var email2 = Email.Create("user@example.com");

            // Assert
            email1.Should().Be(email2);
            (email1 == email2).Should().BeTrue();
        }

        [Fact]
        public void TwoEmailObjects_WithDifferentValues_ShouldNotBeEqual()
        {
            // Arrange
            var email1 = Email.Create("user1@example.com");
            var email2 = Email.Create("user2@example.com");

            // Assert
            email1.Should().NotBe(email2);
            (email1 != email2).Should().BeTrue();
        }

        [Fact]
        public void TwoEmailObjects_WithSameValueButDifferentCase_ShouldBeEqual()
        {
            // Arrange
            var email1 = Email.Create("User@Example.COM");
            var email2 = Email.Create("user@example.com");

            // Assert
            email1.Should().Be(email2);
        }

        #endregion

        #region ToString Tests

        [Fact]
        public void ToString_ShouldReturnEmailValue()
        {
            // Arrange
            var email = Email.Create("test@example.com");

            // Act
            var result = email.ToString();

            // Assert
            result.Should().Be("test@example.com");
        }

        #endregion
    }
}
