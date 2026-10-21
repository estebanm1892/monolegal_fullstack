using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Domain.Reminders;

/// <summary>
/// Audit-facing reason for a reminder decision, independent from persisted state text.
/// </summary>
public enum ReminderOutcome
{
    Notify = 0,
    AlreadyDeactivated = 1,
    NotEligible = 2,
    UnknownState = 3,
}

/// <summary>
/// Result of evaluating the transition policy for one invoice. At most one step is returned.
/// </summary>
public sealed record ReminderDecision
{
    private ReminderDecision(bool shouldNotify, ReminderOutcome outcome, InvoiceState? nextState)
    {
        ShouldNotify = shouldNotify;
        Outcome = outcome;
        NextState = nextState;
    }

    public bool ShouldNotify { get; }

    public ReminderOutcome Outcome { get; }

    public InvoiceState? NextState { get; }

    public static ReminderDecision Notify(InvoiceState nextState) =>
        nextState is null
            ? throw new ArgumentNullException(nameof(nextState))
            : new ReminderDecision(true, ReminderOutcome.Notify, nextState);

    public static ReminderDecision Skip(ReminderOutcome outcome) =>
        outcome == ReminderOutcome.Notify
            ? throw new ArgumentOutOfRangeException(nameof(outcome), "A skip decision cannot carry the Notify outcome.")
            : new ReminderDecision(false, outcome, null);
}
