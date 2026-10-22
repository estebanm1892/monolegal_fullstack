import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
} from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { InvoicesService, InvoiceQuery } from './invoices.service';
import { API_CONFIG } from './api-config.token';
import { DEFAULT_API_CONFIG } from './api-config';

describe('InvoicesService', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let service: InvoicesService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: { baseUrl: 'http://test-api:5091' } },
        InvoicesService,
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    service = TestBed.inject(InvoicesService);
  });

  afterEach(() => {
    controller.verify();
    TestBed.resetTestingModule();
  });

  it('GET /api/invoices with filters', () => {
    const query: InvoiceQuery = {
      page: 1,
      pageSize: 10,
      customerId: 'cliente-a',
      status: 'pendiente',
    };
    const expected = {
      items: [],
      page: 1,
      pageSize: 10,
      totalCount: 0,
      totalPages: 0,
    };

    service.getInvoices(query).subscribe((got) => {
      expect(got).toEqual(expected);
    });

    const req = controller.expectOne((r) => r.url?.includes('/api/invoices'));
    expect(req.request.method).toBe('GET');
    expect(req.request.url).toContain('page=1');
    expect(req.request.url).toContain('pageSize=10');
    expect(req.request.url).toContain('customerId=cliente-a');
    expect(req.request.url).toContain('status=pendiente');
    req.flush(expected);
  });

  it('GET /api/invoices/summary', () => {
    const expected = {
      totalInvoices: 6,
      countByStatus: { primerrecordatorio: 2 },
      countByCustomer: { 'cliente-a': 2 },
      totalAmountCop: 150000,
    };

    service.getSummary().subscribe((got) => {
      expect(got).toEqual(expected);
    });

    const req = controller.expectOne('http://test-api:5091/api/invoices/summary');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });
});
