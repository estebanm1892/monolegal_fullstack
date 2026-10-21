using Monolegal.Billing.Application;
using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Application.Reminders;
using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.UnitTests.Reminders;

public class ProcessInvoiceRemindersTests
{
    [Fact]
    public async Task SuccessfulBatchDeduplicatesSnapshotAndCompletesOneTransitionPerInvoice()
    {
        var fixture = new Fixture(
            Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio),
            Invoice("FAC-002", "customer-b", InvoiceState.SegundoRecordatorio));
        fixture.Invoices.EligibleIds.Add(new InvoiceId("FAC-001"));

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal("batch-1", result.BatchId);
        Assert.Equal(2, result.TotalCandidates);
        Assert.Equal(2, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.NeedsReview);
        Assert.Equal(2, fixture.Email.Messages.Count);
        Assert.Equal(2, fixture.Email.Messages.Select(message => message.Subject).Distinct().Count());
        Assert.Equal(InvoiceState.SegundoRecordatorio, fixture.Invoices.Stored["FAC-001"].State);
        Assert.Equal(InvoiceState.Desactivado, fixture.Invoices.Stored["FAC-002"].State);
        Assert.All(fixture.Audit.Stored.Values, attempt => Assert.Equal(NotificationAttemptStatus.Completed, attempt.Status));
    }

    [Fact]
    public async Task UnavailableClaimIsSkippedWithoutSending()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Invoices.UnavailableClaims.Add("FAC-001");

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Skipped);
        Assert.Empty(fixture.Email.Messages);
        Assert.Empty(fixture.Audit.Stored);
    }

    [Fact]
    public async Task MissingCustomerFailsReleasesClaimAndDoesNotSend()
    {
        var fixture = new Fixture(Invoice("FAC-001", "missing", InvoiceState.PrimerRecordatorio));

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Equal(1, fixture.Invoices.ReleaseCount);
        Assert.Empty(fixture.Email.Messages);
        Assert.Equal(NotificationAttemptStatus.Failed, Assert.Single(fixture.Audit.Stored.Values).Status);
        Assert.Equal(InvoiceState.PrimerRecordatorio, fixture.Invoices.Stored["FAC-001"].State);
    }

    [Fact]
    public async Task DispatchAuditFailurePreventsSmtpAndReleasesClaim()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Audit.FailUpdatesFor.Add(NotificationAttemptStatus.Dispatching);

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Empty(fixture.Email.Messages);
        Assert.Equal(1, fixture.Invoices.ReleaseCount);
        Assert.Equal(0, fixture.Invoices.BeginDispatchCount);
    }

    [Fact]
    public async Task AuditCreationFailurePreventsSmtpAndReleasesClaim()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Audit.FailAdd = true;

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Empty(fixture.Email.Messages);
        Assert.Equal(1, fixture.Invoices.ReleaseCount);
        Assert.Empty(fixture.Audit.Stored);
    }

    [Fact]
    public async Task KnownSmtpRejectionFailsReleasesClaimAndKeepsState()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Email.NextResult = EmailSendResult.KnownNotAccepted("smtp_rejected", "Rejected by local SMTP.");

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Equal(1, fixture.Invoices.ReleaseCount);
        Assert.Equal(0, fixture.Invoices.CompleteCount);
        Assert.Equal(InvoiceState.PrimerRecordatorio, fixture.Invoices.Stored["FAC-001"].State);
        Assert.Equal(NotificationAttemptStatus.Failed, Assert.Single(fixture.Audit.Stored.Values).Status);
    }

    [Fact]
    public async Task AmbiguousSmtpResultNeedsReviewWithoutReleaseOrTransition()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Email.NextResult = EmailSendResult.AcceptanceUnknown("smtp_acceptance_unknown", "No reliable response.");

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NeedsReview);
        Assert.Equal(0, fixture.Invoices.ReleaseCount);
        Assert.Equal(0, fixture.Invoices.CompleteCount);
        Assert.Equal(NotificationAttemptStatus.NeedsReview, Assert.Single(fixture.Audit.Stored.Values).Status);
    }

    [Fact]
    public async Task UnexpectedSenderExceptionNeedsReviewWithoutReleaseOrTransition()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Email.ExceptionToThrow = new IOException("Simulated transport failure.");

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NeedsReview);
        Assert.Equal(0, fixture.Invoices.ReleaseCount);
        Assert.Equal(0, fixture.Invoices.CompleteCount);
        Assert.Equal(NotificationAttemptStatus.NeedsReview, Assert.Single(fixture.Audit.Stored.Values).Status);
    }

    [Fact]
    public async Task AcceptedEmailWithoutDurableAcceptanceNeedsReviewAndSkipsCas()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Audit.FailUpdatesFor.Add(NotificationAttemptStatus.SmtpAccepted);

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NeedsReview);
        Assert.Single(fixture.Email.Messages);
        Assert.Equal(0, fixture.Invoices.CompleteCount);
        Assert.Equal(0, fixture.Invoices.ReleaseCount);
        var attempt = Assert.Single(fixture.Audit.Stored.Values);
        Assert.Equal(NotificationAttemptStatus.NeedsReview, attempt.Status);
        Assert.NotNull(attempt.SmtpAcceptedAt);
        Assert.Equal("message-1@example.test", attempt.MessageId);
    }

    [Fact]
    public async Task AcceptedEmailWithCasFailureNeedsReviewAndIsNotRetried()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Invoices.FailedCompletions.Add("FAC-001");

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NeedsReview);
        Assert.Single(fixture.Email.Messages);
        Assert.Equal(1, fixture.Invoices.CompleteCount);
        Assert.Equal(0, fixture.Invoices.ReleaseCount);
        Assert.Equal(NotificationAttemptStatus.NeedsReview, Assert.Single(fixture.Audit.Stored.Values).Status);
    }

    [Fact]
    public async Task CompletionAuditFailureNeedsReviewAfterStateTransition()
    {
        var fixture = new Fixture(Invoice("FAC-001", "customer-a", InvoiceState.PrimerRecordatorio));
        fixture.Audit.FailUpdatesFor.Add(NotificationAttemptStatus.Completed);

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NeedsReview);
        Assert.Equal(InvoiceState.SegundoRecordatorio, fixture.Invoices.Stored["FAC-001"].State);
        Assert.Equal(NotificationAttemptStatus.NeedsReview, Assert.Single(fixture.Audit.Stored.Values).Status);
    }

    [Fact]
    public async Task OneInvoiceFailureDoesNotStopSafeRemainingCandidate()
    {
        var fixture = new Fixture(
            Invoice("FAC-001", "missing", InvoiceState.PrimerRecordatorio),
            Invoice("FAC-002", "customer-b", InvoiceState.SegundoRecordatorio));

        var result = await fixture.Sut.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Equal(1, result.Succeeded);
        Assert.Single(fixture.Email.Messages);
        Assert.Equal("customer-b@example.test", fixture.Email.Messages[0].RecipientAddress);
    }

    private static Invoice Invoice(string id, string customerId, InvoiceState state) => new(
        new InvoiceId(id),
        new CustomerId(customerId),
        id,
        new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
        100000m,
        state);

    private sealed class Fixture
    {
        public Fixture(params Invoice[] invoices)
        {
            Invoices = new FakeInvoiceRepository(invoices);
            Customers = new FakeCustomerRepository(
                Customer("customer-a"),
                Customer("customer-b"),
                Customer("customer-c"));
            Audit = new FakeAuditRepository();
            Email = new FakeEmailSender();
            Sut = new ProcessInvoiceReminders(
                Invoices,
                Customers,
                Audit,
                Email,
                new FixedTimeSource(),
                new SequentialReminderIdGenerator());
        }

        public FakeInvoiceRepository Invoices { get; }
        public FakeCustomerRepository Customers { get; }
        public FakeAuditRepository Audit { get; }
        public FakeEmailSender Email { get; }
        public ProcessInvoiceReminders Sut { get; }

        private static Customer Customer(string id) => new(
            new CustomerId(id),
            $"Customer {id[^1]}",
            EmailAddress.Parse($"{id}@example.test"),
            true);
    }

    private sealed class FixedTimeSource : ITimeSource
    {
        public DateTimeOffset UtcNow => new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequentialReminderIdGenerator : IReminderIdGenerator
    {
        private int _batch;
        private int _attempt;
        private int _claim;

        public string NewBatchId() => $"batch-{++_batch}";
        public string NewAttemptId() => $"attempt-{++_attempt}";
        public string NewClaimToken() => $"claim-{++_claim}";
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];
        public EmailSendResult NextResult { get; set; } = EmailSendResult.Accepted("message-1@example.test");
        public Exception? ExceptionToThrow { get; set; }

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);

            if (ExceptionToThrow is not null)
            {
                throw ExceptionToThrow;
            }

            return Task.FromResult(NextResult);
        }
    }

    private sealed class FakeCustomerRepository(params Customer[] customers) : ICustomerRepository
    {
        private readonly Dictionary<string, Customer> _customers = customers.ToDictionary(customer => customer.Id.Value);

        public Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>(_customers.Values.ToArray());

        public Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(_customers.GetValueOrDefault(id.Value));
    }

    private sealed class FakeAuditRepository : INotificationAuditRepository
    {
        public Dictionary<string, NotificationAttempt> Stored { get; } = [];
        public HashSet<NotificationAttemptStatus> FailUpdatesFor { get; } = [];
        public bool FailAdd { get; set; }

        public Task AddAsync(NotificationAttempt attempt, CancellationToken cancellationToken)
        {
            if (FailAdd)
            {
                throw new IOException("Simulated audit write failure.");
            }

            Stored.Add(attempt.AttemptId, attempt);
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(NotificationAttempt attempt, CancellationToken cancellationToken)
        {
            if (FailUpdatesFor.Contains(attempt.Status))
            {
                return Task.FromResult(false);
            }

            Stored[attempt.AttemptId] = attempt;
            return Task.FromResult(true);
        }

        public Task<NotificationAttempt?> FindAsync(string attemptId, CancellationToken cancellationToken) =>
            Task.FromResult(Stored.GetValueOrDefault(attemptId));
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        public FakeInvoiceRepository(IEnumerable<Invoice> invoices)
        {
            Stored = invoices.ToDictionary(invoice => invoice.Id.Value);
            EligibleIds.AddRange(Stored.Values.Select(invoice => invoice.Id));
        }

        public Dictionary<string, Invoice> Stored { get; }
        public List<InvoiceId> EligibleIds { get; } = [];
        public HashSet<string> UnavailableClaims { get; } = [];
        public HashSet<string> FailedCompletions { get; } = [];
        public int BeginDispatchCount { get; private set; }
        public int CompleteCount { get; private set; }
        public int ReleaseCount { get; private set; }

        public Task<Invoice?> FindByIdAsync(InvoiceId id, CancellationToken cancellationToken) =>
            Task.FromResult(Stored.GetValueOrDefault(id.Value));

        public Task<IReadOnlyList<InvoiceId>> FindEligibleIdsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InvoiceId>>(EligibleIds.ToArray());

        public Task<InvoiceClaim?> TryClaimAsync(
            InvoiceId id,
            InvoiceState expectedState,
            InvoiceClaimRequest request,
            CancellationToken cancellationToken) =>
            Task.FromResult(UnavailableClaims.Contains(id.Value)
                ? null
                : new InvoiceClaim(request.Token, ClaimPhases.Claimed, request.StartedAt, request.LockUntil));

        public Task<bool> TryBeginDispatchAsync(
            InvoiceId id,
            string token,
            DateTimeOffset lockUntil,
            CancellationToken cancellationToken)
        {
            BeginDispatchCount++;
            return Task.FromResult(true);
        }

        public Task<bool> TryCompleteTransitionAsync(
            InvoiceId id,
            InvoiceState expectedState,
            string token,
            InvoiceState nextState,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken)
        {
            CompleteCount++;

            if (FailedCompletions.Contains(id.Value))
            {
                return Task.FromResult(false);
            }

            var current = Stored[id.Value];
            Stored[id.Value] = new Invoice(
                current.Id,
                current.CustomerId,
                current.Number,
                current.IssuedAt,
                current.DueAt,
                updatedAt,
                current.Amount,
                nextState);
            return Task.FromResult(true);
        }

        public Task<bool> ReleaseClaimAsync(InvoiceId id, string token, CancellationToken cancellationToken)
        {
            ReleaseCount++;
            return Task.FromResult(true);
        }

        public Task<PagedResult<Invoice>> ListAsync(InvoiceQuery query, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<InvoiceSummary> GetSummaryAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
