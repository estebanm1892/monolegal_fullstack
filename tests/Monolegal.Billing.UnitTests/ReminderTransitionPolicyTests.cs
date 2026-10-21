using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Domain.Reminders;

namespace Monolegal.Billing.UnitTests;

public class ReminderTransitionPolicyTests
{
    private readonly ReminderTransitionPolicy _policy = ReminderTransitionPolicy.Default;

    [Fact]
    public void FirstReminderAdvancesToSecondReminder()
    {
        var decision = _policy.Evaluate(InvoiceState.PrimerRecordatorio);

        Assert.True(decision.ShouldNotify);
        Assert.Equal(ReminderOutcome.Notify, decision.Outcome);
        Assert.Equal(InvoiceState.SegundoRecordatorio, decision.NextState);
    }

    [Fact]
    public void SecondReminderAdvancesToDeactivated()
    {
        var decision = _policy.Evaluate(InvoiceState.SegundoRecordatorio);

        Assert.True(decision.ShouldNotify);
        Assert.Equal(ReminderOutcome.Notify, decision.Outcome);
        Assert.Equal(InvoiceState.Desactivado, decision.NextState);
    }

    [Fact]
    public void DeactivatedHasNoTransition()
    {
        var decision = _policy.Evaluate(InvoiceState.Desactivado);

        Assert.False(decision.ShouldNotify);
        Assert.Equal(ReminderOutcome.AlreadyDeactivated, decision.Outcome);
        Assert.Null(decision.NextState);
    }

    [Fact]
    public void PendingHasNoTransition()
    {
        var decision = _policy.Evaluate(InvoiceState.Pendiente);

        Assert.False(decision.ShouldNotify);
        Assert.Equal(ReminderOutcome.NotEligible, decision.Outcome);
        Assert.Null(decision.NextState);
    }

    [Fact]
    public void UnknownStateHasNoTransition()
    {
        var decision = _policy.Evaluate(InvoiceState.FromPersisted("archivada"));

        Assert.False(decision.ShouldNotify);
        Assert.Equal(ReminderOutcome.UnknownState, decision.Outcome);
        Assert.Null(decision.NextState);
    }

    [Fact]
    public void AdvancesAtMostOneStepPerDecision()
    {
        var first = _policy.Evaluate(InvoiceState.PrimerRecordatorio);
        var second = _policy.Evaluate(first.NextState!);

        Assert.NotEqual(InvoiceState.Desactivado, first.NextState);
        Assert.Equal(InvoiceState.SegundoRecordatorio, first.NextState);
        Assert.Equal(InvoiceState.Desactivado, second.NextState);
        Assert.Equal(ReminderOutcome.Notify, second.Outcome);
    }

    [Fact]
    public void PolicyIsImmutableAndShared()
    {
        Assert.Same(ReminderTransitionPolicy.Default, ReminderTransitionPolicy.Default);
        Assert.DoesNotContain(
            typeof(ReminderTransitionPolicy).GetProperties(),
            property => property.CanWrite && property.SetMethod!.IsPublic);
    }
}
