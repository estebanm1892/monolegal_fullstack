using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Domain.Reminders;

/// <summary>
/// Immutable business rule for reminder transitions. Exactly one step per evaluation:
/// primerrecordatorio -> segundorecordatorio, segundorecordatorio -> desactivado.
/// </summary>
public sealed class ReminderTransitionPolicy
{
    private ReminderTransitionPolicy()
    {
    }

    public static ReminderTransitionPolicy Default { get; } = new();

    public ReminderDecision Evaluate(InvoiceState current)
    {
        ArgumentNullException.ThrowIfNull(current);

        return current.Kind switch
        {
            InvoiceStateKind.PrimerRecordatorio => ReminderDecision.Notify(InvoiceState.SegundoRecordatorio),
            InvoiceStateKind.SegundoRecordatorio => ReminderDecision.Notify(InvoiceState.Desactivado),
            InvoiceStateKind.Desactivado => ReminderDecision.Skip(ReminderOutcome.AlreadyDeactivated),
            InvoiceStateKind.Pendiente => ReminderDecision.Skip(ReminderOutcome.NotEligible),
            _ => ReminderDecision.Skip(ReminderOutcome.UnknownState),
        };
    }
}
