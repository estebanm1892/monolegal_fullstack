import { TestBed } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { DashboardComponent } from './dashboard.component';
import { DashboardService, ProcessingResultRow } from './dashboard.service';
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
    page: signal(1),
    totalPages: signal(0),
    totalCount: signal(0),
    customersLoading: signal(false),
    invoicesLoading: signal(false),
    summaryLoading: signal(false),
    runDetailsLoading: signal(false),
    customersError: signal<string | null>(null),
    invoicesError: signal<string | null>(null),
    summaryError: signal<string | null>(null),
    processingError: signal<string | null>(null),
    runDetailsError: signal<string | null>(null),
    runDetails: signal([]),
    executionRows: signal([] as ProcessingResultRow[]),
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

  function setExecutionRows(rows: ProcessingResultRow[]): void {
    (service.executionRows as WritableSignal<readonly ProcessingResultRow[]>).set(rows);
  }

  it('renders summary cards', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    service.summary.set({
      totalInvoices: 6,
      countByStatus: {
        primerrecordatorio: 2,
        segundorecordatorio: 2,
        pendiente: 1,
        desactivado: 1,
      },
      countByCustomer: { 'cliente-a': 2, 'cliente-b': 2, 'cliente-c': 2 },
      totalAmountCop: 1060000,
    });
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('6');
    expect(page.textContent).toContain('1.060.000');
    expect(page.textContent).toContain('Pendiente');
    expect(page.textContent).toContain('Resumen global');
  });

  it('explains that filters do not limit global processing', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();

    expect(fixture.nativeElement.textContent).toContain('Los filtros solo modifican esta tabla');
    expect(fixture.nativeElement.textContent).toContain('todas las facturas elegibles');
  });

  it('renders invoice table rows', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    service.invoices.set([
      {
        id: 'FAC-001',
        customerId: 'cliente-a',
        number: 'FAC-001',
        issuedAt: '2026-09-01T00:00:00Z',
        dueAt: '2026-09-10T00:00:00Z',
        updatedAt: '2026-10-01T12:00:00Z',
        amount: 150000,
        currency: 'COP',
        state: 'primerrecordatorio',
      },
    ]);
    service.customers.set([
      { id: 'cliente-a', name: 'Cliente A', email: 'a@example.test', active: true },
    ]);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('FAC-001');
    expect(page.textContent).toContain('Cliente A');
    expect(page.textContent).toContain('150.000');
    expect(page.textContent).toContain('10/09/2026');
    expect(page.textContent).toContain('Primer recordatorio');
  });

  it('renders real pagination controls', async () => {
    service.page.set(2);
    service.totalPages.set(3);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('Página 2 de 3');
    const buttons = Array.from(page.querySelectorAll('.pagination button')) as HTMLButtonElement[];
    expect(buttons).toHaveLength(2);
    expect(buttons[0].disabled).toBe(false);
    expect(buttons[1].disabled).toBe(false);
  });

  it('hides pagination when there is a single page', async () => {
    service.page.set(1);
    service.totalPages.set(1);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('.pagination')).toBeNull();
  });

  it('disables process button while processing', async () => {
    service.processing.set(true);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
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
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    const alert = page.querySelector('.alert-review');
    expect(alert).toBeTruthy();
    expect(page.textContent).toContain('revisión');
  });

  it('renders the latest execution as a table with real invoice and audit data', async () => {
    service.result.set({
      batchId: 'batch-123456789012345',
      totalCandidates: 1,
      succeeded: 1,
      failed: 0,
      skipped: 0,
      needsReview: 0,
      results: [],
    });
    setExecutionRows([
      {
        invoiceId: 'invoice-1',
        invoiceNumber: 'FAC-001',
        customerName: 'Cliente A',
        previousState: 'primerrecordatorio',
        nextState: 'segundorecordatorio',
        status: 'Succeeded',
        failureCode: null,
        details: null,
      },
    ]);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('Última ejecución');
    expect(page.textContent).not.toContain('Lote batch-');
    expect(page.textContent).toContain('Facturas actualizadas');
    expect(page.textContent).toContain('FAC-001');
    expect(page.textContent).toContain('Cliente A');
    expect(page.textContent).toContain('Primer recordatorio');
    expect(page.textContent).toContain('Segundo recordatorio');
    expect(page.textContent).toContain('Correo enviado y estado actualizado');
    expect(page.querySelector('.result-item')).toBeNull();

    const technicalDetails = page.querySelector('.technical-details');
    expect(technicalDetails?.textContent).toContain('batch-123456789012345');
  });

  it.each([
    ['Failed', 'No se pudo procesar', 'smtp_rejected'],
    ['Skipped', 'Omitida', 'claim_unavailable'],
    ['NeedsReview', 'Requiere revisión manual', 'smtp_ambiguous'],
  ])('renders %s outcomes without claiming success', async (status, expected, failureCode) => {
    service.result.set({
      batchId: 'batch-2',
      totalCandidates: 1,
      succeeded: 0,
      failed: status === 'Failed' ? 1 : 0,
      skipped: status === 'Skipped' ? 1 : 0,
      needsReview: status === 'NeedsReview' ? 1 : 0,
      results: [],
    });
    setExecutionRows([
      {
        invoiceId: 'invoice-2',
        invoiceNumber: 'FAC-002',
        customerName: 'Cliente A',
        previousState: status === 'Skipped' ? null : 'segundorecordatorio',
        nextState: status === 'Skipped' ? null : 'desactivado',
        status,
        failureCode,
        details: null,
      },
    ]);

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain(expected);
    expect(page.textContent).not.toContain('Correo enviado y estado actualizado');
    if (status === 'Skipped') {
      expect(page.textContent).toContain('No disponible');
    }
  });

  it('shows an understandable empty state when no invoices were eligible', async () => {
    service.result.set({
      batchId: 'batch-empty',
      totalCandidates: 0,
      succeeded: 0,
      failed: 0,
      skipped: 0,
      needsReview: 0,
      results: [],
    });

    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.textContent).toContain('No había facturas elegibles');
    expect(page.querySelector('.result-table')).toBeNull();
    expect(page.querySelector('.result-item')).toBeNull();
  });

  it('shows expected global transitions before processing', async () => {
    service.summary.set({
      totalInvoices: 6,
      countByStatus: { primerrecordatorio: 2, segundorecordatorio: 2 },
      countByCustomer: {},
      totalAmountCop: 0,
    });
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();

    const processButton = fixture.nativeElement.querySelector(
      '[data-testid="process-btn"]',
    ) as HTMLButtonElement;
    processButton.click();
    await fixture.whenStable();

    const dialog = fixture.nativeElement.querySelector('[role="alertdialog"]');
    expect(dialog).toBeTruthy();
    expect(dialog.textContent).toContain('2 pasarán de Primer recordatorio a Segundo recordatorio');
    expect(dialog.textContent).toContain('2 pasarán de Segundo recordatorio a Desactivada');
    expect(dialog.textContent).toContain('4 correos');
    expect((service as any).processRemindersWasCalled).toBe(false);

    const confirmButton = fixture.nativeElement.querySelector(
      '[data-testid="confirm-process-btn"]',
    ) as HTMLButtonElement;
    confirmButton.click();

    expect((service as any).processRemindersWasCalled).toBe(true);
  });

  it('uses semantic headings and an accessible table region', async () => {
    const fixture = TestBed.createComponent(DashboardComponent);
    fixture.autoDetectChanges(true);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('h1')).toBeTruthy();
    const tableRegion = fixture.nativeElement.querySelector('.table-scroll');
    expect(tableRegion?.getAttribute('tabindex')).toBe('0');
    expect(tableRegion?.getAttribute('aria-label')).toContain('desplazamiento');
  });
});
