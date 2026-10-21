using Monolegal.Billing.Domain.Customers;

namespace Monolegal.Billing.UnitTests;

public class CustomerTests
{
    [Theory]
    [InlineData("cliente-a@example.test")]
    [InlineData("persona.apellido@example.test")]
    [InlineData("a@b.co")]
    public void AcceptsSyntacticallyValidEmails(string email)
    {
        Assert.True(EmailAddress.TryParse(email, out var parsed));
        Assert.NotNull(parsed);
        Assert.Equal(email, parsed.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin-arroba.example.test")]
    [InlineData("dos@@example.test")]
    [InlineData("usuario@")]
    [InlineData("@example.test")]
    [InlineData("usuario@example")]
    [InlineData("usuario con espacio@example.test")]
    [InlineData(".usuario@example.test")]
    [InlineData("usuario.@example.test")]
    [InlineData("usuario@.example.test")]
    [InlineData("usuario@example..test")]
    public void RejectsInvalidEmails(string? email)
    {
        Assert.False(EmailAddress.TryParse(email, out var parsed));
        Assert.Null(parsed);
    }

    [Fact]
    public void ParseThrowsForInvalidEmail()
    {
        Assert.Throws<FormatException>(() => EmailAddress.Parse("no-es-email"));
    }

    [Fact]
    public void CreatesCustomerWithRequiredData()
    {
        var customer = new Customer(
            new CustomerId("cliente-a"),
            "Cliente A",
            EmailAddress.Parse("cliente-a@example.test"),
            active: true);

        Assert.Equal("cliente-a", customer.Id.Value);
        Assert.Equal("Cliente A", customer.Name);
        Assert.Equal("cliente-a@example.test", customer.Email.Value);
        Assert.True(customer.Active);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyName(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Customer(
            new CustomerId("cliente-a"),
            name!,
            EmailAddress.Parse("cliente-a@example.test"),
            active: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyCustomerId(string? id)
    {
        Assert.Throws<ArgumentException>(() => new CustomerId(id!));
    }
}
