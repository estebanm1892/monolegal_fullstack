import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { CustomersService } from '../../core/api/customers.service';
import { InvoicesService } from '../../core/api/invoices.service';
import { RemindersService } from '../../core/api/reminders.service';

describe('DashboardService', () => {
  let invoicesService: {
    getInvoices: () => unknown;
    getSummary: () => unknown;
  };
  let remindersService: {
    processReminders: () => unknown;
    getRun: (batchId: string) => unknown;
  };
  let service: DashboardService;

  const page2 = {
    items: [],
    page: 2,
    pageSize: 10,
    totalCount: 15,
    totalPages: 2,
  };

  const summary = {
    totalInvoices: 15,
    countByStatus: {},
    countByCustomer: {},
    totalAmountCop: 0,
  };

  beforeEach(() => {
    TestBed.resetTestingModule();
    invoicesService = {
      getInvoices: () => of(page2),
      getSummary: () => of(summary),
    };
    remindersService = {
      processReminders: () =>
        of({
          batchId: 'batch-1',
          totalCandidates: 1,
          succeeded: 1,
          failed: 0,
          skipped: 0,
          needsReview: 0,
          results: [
            {
              invoiceId: 'invoice-1',
              status: 'Succeeded',
              attemptId: 'attempt-1',
              failureCode: null,
              details: null,
            },
          ],
        }),
      getRun: () => of([]),
    };

    TestBed.configureTestingModule({
      providers: [
        DashboardService,
        { provide: CustomersService, useValue: { getCustomers: () => of([]) } },
        { provide: InvoicesService, useValue: invoicesService },
        { provide: RemindersService, useValue: remindersService },
      ],
    });
    service = TestBed.inject(DashboardService);
  });

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('stores pagination metadata from the list response', () => {
    service.loadInvoices();

    expect(service.page()).toBe(2);
    expect(service.totalPages()).toBe(2);
    expect(service.totalCount()).toBe(15);
  });

  it('refreshes invoices and summary after processing a batch', () => {
    let invoiceCalls = 0;
    let summaryCalls = 0;
    invoicesService.getInvoices = () => {
      invoiceCalls += 1;
      return of(page2);
    };
    invoicesService.getSummary = () => {
      summaryCalls += 1;
      return of(summary);
    };

    service.processReminders();

    expect(invoiceCalls).toBe(2);
    expect(summaryCalls).toBe(1);
    expect(service.processing()).toBe(false);
    expect(service.result()?.batchId).toBe('batch-1');
  });

  it('ignores page navigation outside the available range', () => {
    service.loadInvoices();
    let calls = 0;
    invoicesService.getInvoices = () => {
      calls += 1;
      return of(page2);
    };

    service.setPage(3);
    service.setPage(0);

    expect(calls).toBe(0);
  });

  it('keeps loading state isolated between concurrent resources', () => {
    const customers = new Subject<never[]>();
    const invoices = new Subject<typeof page2>();
    const customerService = TestBed.inject(CustomersService) as unknown as {
      getCustomers: () => unknown;
    };
    customerService.getCustomers = () => customers;
    invoicesService.getInvoices = () => invoices;

    service.loadCustomers();
    service.loadInvoices();
    customers.next([]);
    customers.complete();

    expect(service.customersLoading()).toBe(false);
    expect(service.invoicesLoading()).toBe(true);

    invoices.next(page2);
    invoices.complete();
    expect(service.invoicesLoading()).toBe(false);
  });

  it('ignores an obsolete invoice response after filters change', () => {
    const first = new Subject<typeof page2>();
    const second = new Subject<typeof page2>();
    let calls = 0;
    invoicesService.getInvoices = () => (calls++ === 0 ? first : second);

    service.loadInvoices();
    service.setFilter('status', 'pendiente');
    second.next({ ...page2, page: 1, totalCount: 1 });
    second.complete();
    first.next({ ...page2, page: 1, totalCount: 99 });
    first.complete();

    expect(service.totalCount()).toBe(1);
  });

  it('loads the run detail after a successful process', () => {
    let requestedBatch = '';
    remindersService.getRun = (batchId: string) => {
      requestedBatch = batchId;
      return of([
        {
          attemptId: 'attempt-1',
          invoiceId: 'invoice-1',
          previousState: 'primerrecordatorio',
          nextState: 'segundorecordatorio',
          status: 'Completed',
          startedAt: null,
          smtpAcceptedAt: null,
          finishedAt: null,
          messageId: null,
          failureCode: null,
          details: null,
        },
      ]);
    };

    service.processReminders();

    expect(requestedBatch).toBe('batch-1');
    expect(service.runDetails()).toHaveLength(1);
  });

  it('keeps POST results primary and enriches them with audit and invoice metadata', () => {
    remindersService.processReminders = () =>
      of({
        batchId: 'batch-enriched',
        totalCandidates: 1,
        succeeded: 1,
        failed: 0,
        skipped: 0,
        needsReview: 0,
        results: [
          {
            invoiceId: 'invoice-1',
            status: 'Succeeded',
            attemptId: 'attempt-1',
            failureCode: null,
            details: null,
          },
        ],
      });
    remindersService.getRun = () =>
      of([
        {
          attemptId: 'attempt-1',
          invoiceId: 'invoice-1',
          previousState: 'primerrecordatorio',
          nextState: 'segundorecordatorio',
          status: 'Completed',
          startedAt: null,
          smtpAcceptedAt: null,
          finishedAt: null,
          messageId: null,
          failureCode: null,
          details: null,
        },
      ]);
    invoicesService.getInvoices = () =>
      of({
        items: [
          {
            id: 'invoice-1',
            customerId: 'customer-1',
            number: 'FAC-001',
            issuedAt: '2026-09-01T00:00:00Z',
            dueAt: '2026-09-10T00:00:00Z',
            updatedAt: '2026-10-22T12:00:00Z',
            amount: 100,
            currency: 'COP',
            state: 'segundorecordatorio',
          },
        ],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
      });
    service.customers.set([
      { id: 'customer-1', name: 'Cliente A', email: 'a@example.test', active: true },
    ]);

    service.processReminders();

    expect(service.executionRows()).toEqual([
      expect.objectContaining({
        invoiceNumber: 'FAC-001',
        customerName: 'Cliente A',
        previousState: 'primerrecordatorio',
        nextState: 'segundorecordatorio',
        status: 'Succeeded',
      }),
    ]);
  });

  it('ignores audit responses from an older execution', () => {
    const firstAudit = new Subject<never[]>();
    let processCall = 0;
    remindersService.processReminders = () => {
      processCall += 1;
      return of({
        batchId: `batch-${processCall}`,
        totalCandidates: 1,
        succeeded: 1,
        failed: 0,
        skipped: 0,
        needsReview: 0,
        results: [
          {
            invoiceId: `invoice-${processCall}`,
            status: 'Succeeded',
            attemptId: `attempt-${processCall}`,
            failureCode: null,
            details: null,
          },
        ],
      });
    };
    remindersService.getRun = (batchId: string) => (batchId === 'batch-1' ? firstAudit : of([]));

    service.processReminders();
    service.processReminders();
    firstAudit.next([]);
    firstAudit.complete();

    expect(service.result()?.batchId).toBe('batch-2');
    expect(service.runDetails()).toEqual([]);
    expect(service.runDetailsLoading()).toBe(false);
  });

  it('refreshes read models without retrying when processing fails', () => {
    let processCalls = 0;
    let invoiceCalls = 0;
    let summaryCalls = 0;
    remindersService.processReminders = () => {
      processCalls += 1;
      return throwError(() => new Error('network unavailable'));
    };
    invoicesService.getInvoices = () => {
      invoiceCalls += 1;
      return of(page2);
    };
    invoicesService.getSummary = () => {
      summaryCalls += 1;
      return of(summary);
    };

    service.processReminders();

    expect(processCalls).toBe(1);
    expect(invoiceCalls).toBe(1);
    expect(summaryCalls).toBe(1);
    expect(service.processingError()).toContain('No se pudo confirmar');
  });
});
