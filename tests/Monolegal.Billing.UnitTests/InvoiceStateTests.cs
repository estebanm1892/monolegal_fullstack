using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.UnitTests;

public class InvoiceStateTests
{
    [Theory]
    [InlineData("pendiente", InvoiceStateKind.Pendiente)]
    [InlineData("primerrecordatorio", InvoiceStateKind.PrimerRecordatorio)]
    [InlineData("segundorecordatorio", InvoiceStateKind.SegundoRecordatorio)]
    [InlineData("desactivado", InvoiceStateKind.Desactivado)]
    public void ParsesKnownPersistedStatesExactly(string persisted, InvoiceStateKind expected)
    {
        var state = InvoiceState.FromPersisted(persisted);

        Assert.Equal(expected, state.Kind);
        Assert.Equal(persisted, state.Persisted);
    }

    [Theory]
    [InlineData("archivada")]
    [InlineData("PrimerRecordatorio")]
    [InlineData(" primerrecordatorio")]
    [InlineData("otro-estado-desconocido")]
    public void PreservesUnknownPersistedStatesVerbatim(string persisted)
    {
        var state = InvoiceState.FromPersisted(persisted);

        Assert.Equal(InvoiceStateKind.Unknown, state.Kind);
        Assert.Equal(persisted, state.Persisted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsMissingPersistedState(string? persisted)
    {
        Assert.Throws<ArgumentException>(() => InvoiceState.FromPersisted(persisted!));
    }

    [Fact]
    public void KnownStatesRoundTripToTheirPersistedText()
    {
        Assert.Equal("pendiente", InvoiceState.Pendiente.Persisted);
        Assert.Equal("primerrecordatorio", InvoiceState.PrimerRecordatorio.Persisted);
        Assert.Equal("segundorecordatorio", InvoiceState.SegundoRecordatorio.Persisted);
        Assert.Equal("desactivado", InvoiceState.Desactivado.Persisted);
    }
}
