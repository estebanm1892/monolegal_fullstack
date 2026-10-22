import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
} from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { CustomersService } from './customers.service';
import { API_CONFIG } from './api-config.token';
import { DEFAULT_API_CONFIG } from './api-config';

describe('CustomersService', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let service: CustomersService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: { baseUrl: 'http://test-api:5091' } },
        CustomersService,
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    service = TestBed.inject(CustomersService);
  });

  afterEach(() => {
    controller.verify();
    TestBed.resetTestingModule();
  });

  it('GET /api/customers', () => {
    const expected: { id: string; name: string; email: string; active: boolean }[] = [
      { id: 'cliente-a', name: 'Cliente A', email: 'cliente-a@example.test', active: true },
    ];

    service.getCustomers().subscribe((got) => {
      expect(got).toEqual(expected);
    });

    const req = controller.expectOne('http://test-api:5091/api/customers');
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });
});
