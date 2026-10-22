import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { DashboardComponent } from './dashboard.component';
import { DashboardService } from './dashboard.service';
import { CustomerDto } from '../../core/models/customer.model';
import { InvoiceDto } from '../../core/models/invoice.model';
import { InvoiceSummaryDto } from '../../core/models/summary.model';
import { ProcessRemindersResult } from '../../core/api/reminders.service';

function makeService(): DashboardService {
  let processRemindersCalled = false;
  return {
    customers: signal([] as CustomerDto[]),
    invoices: signal([] as InvoiceDto[]),
    summary: signal<InvoiceSummaryDto | null>(null),
    loading: signal(false),
    error: signal<string | null>(null),
    processing: signal(false),
    result: signal<ProcessRemindersResult | null>(null),
    loadCustomers: () => {},
    loadInvoices: () => {},
    loadSummary: () => {},
    processReminders: () => {
      processRemindersCalled = true;
    },
    setFilter: () => {},
    setPage: () => {},
    get processRemindersWasCalled() {
      return processRemindersCalled;
    },
  } as unknown as DashboardService;
}

describe('DashboardComponent', () => {
  let service: DashboardService;

  beforeEach(() => {
    service = makeService();
    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [{ provide: DashboardService, useValue: service }],
    });
  });

  it('renders summary cards', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    service.summary.set({
      totalInvoices: 6,
      countByStatus: { primerrecordatorio: 2, segundorecordatorio: 2, pendiente: 1, desactivado: 1 },
      countByCustomer: { 'cliente-a': 2, 'cliente-b': 2, 'cliente-c': 2 },
      totalAmountCop: 1060000,
    });
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('6');
    expect(page.textContent).toContain('1.060.000');
  });

  it('renders invoice table rows', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    service.invoices.set([
      {
        id: 'FAC-001',
        customerId: 'cliente-a',
        number: 'FAC-001',
        issuedAt: '2026-09-01T00:00:00Z',
        dueAt: '2026-09-10T00:00:00Z',
        updatedAt: '2026-10-01T00:00:00Z',
        amount: 150000,
        currency: 'COP',
        state: 'primerrecordatorio',
      },
    ]);
    service.customers.set([
      { id: 'cliente-a', name: 'Cliente A', email: 'a@example.test', active: true },
    ]);
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('FAC-001');
    expect(page.textContent).toContain('Cliente A');
  });

  it('disables process button while processing', async () => {
    service.processing.set(true);

    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    const button = page.querySelector('[data-testid="process-btn"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
    expect(button.textContent).toContain('Procesando');
  });

  it('shows needs review alert when needsReview > 0', async () => {
    service.result.set({
      batchId: 'batch-1',
      totalCandidates: 4,
      succeeded: 3,
      failed: 0,
      skipped: 0,
      needsReview: 1,
      results: [],
    });

    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();
    fixture.detectChanges();
    const page = fixture.nativeElement as HTMLElement;

    const alert = page.querySelector('.alert-review');
    expect(alert).toBeTruthy();
    expect(alert!.textContent).toContain('revisión necesaria');
  });

  it('calls processReminders when invoked', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    await fixture.whenStable();

    service.processReminders();
    expect((service as any).processRemindersWasCalled).toBe(true);
  });
});
