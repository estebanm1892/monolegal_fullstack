namespace Monolegal.Billing.Domain.Invoices;

public enum InvoiceStateKind
{
    Pendiente = 0,
    PrimerRecordatorio = 1,
    SegundoRecordatorio = 2,
    Desactivado = 3,
    Unknown = 4,
}

/// <summary>
/// Business state of an invoice. Keeps the exact persisted text so unknown values
/// are never normalized or reinterpreted (CONSTITUTION C3).
/// </summary>
public sealed record InvoiceState
{
    public const string PendienteText = "pendiente";
    public const string PrimerRecordatorioText = "primerrecordatorio";
    public const string SegundoRecordatorioText = "segundorecordatorio";
    public const string DesactivadoText = "desactivado";

    private InvoiceState(string persisted, InvoiceStateKind kind)
    {
        Persisted = persisted;
        Kind = kind;
    }

    public static InvoiceState Pendiente { get; } = new(PendienteText, InvoiceStateKind.Pendiente);

    public static InvoiceState PrimerRecordatorio { get; } = new(PrimerRecordatorioText, InvoiceStateKind.PrimerRecordatorio);

    public static InvoiceState SegundoRecordatorio { get; } = new(SegundoRecordatorioText, InvoiceStateKind.SegundoRecordatorio);

    public static InvoiceState Desactivado { get; } = new(DesactivadoText, InvoiceStateKind.Desactivado);

    public string Persisted { get; }

    public InvoiceStateKind Kind { get; }

    public static InvoiceState FromPersisted(string persisted)
    {
        if (string.IsNullOrWhiteSpace(persisted))
        {
            throw new ArgumentException("A persisted invoice state is required.", nameof(persisted));
        }

        return persisted switch
        {
            PendienteText => Pendiente,
            PrimerRecordatorioText => PrimerRecordatorio,
            SegundoRecordatorioText => SegundoRecordatorio,
            DesactivadoText => Desactivado,
            _ => new InvoiceState(persisted, InvoiceStateKind.Unknown),
        };
    }

    public override string ToString() => Persisted;
}
