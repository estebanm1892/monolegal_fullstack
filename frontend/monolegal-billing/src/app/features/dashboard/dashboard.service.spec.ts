import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardService } from './dashboard.service';
import { CustomersService } from '../../core/api/customers.service';
import { InvoicesService } from '../../core/api/invoices.service';
import { RemindersService } from '../../core/api/reminders.service';

describe('DashboardService', () => {
  let invoicesService: {
    getInvoices: () => unknown;
    getSummary: () => unknown;
  };
  let remindersService: { processReminders: () => unknown };
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
          results: [],
        }),
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

    expect(invoiceCalls).toBe(1);
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
});
