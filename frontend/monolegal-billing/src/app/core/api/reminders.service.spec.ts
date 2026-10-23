import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { RemindersService } from './reminders.service';
import { API_CONFIG } from './api-config.token';
import { DEFAULT_API_CONFIG } from './api-config';

describe('RemindersService', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let service: RemindersService;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_CONFIG, useValue: { baseUrl: 'http://test-api:5091' } },
        RemindersService,
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    service = TestBed.inject(RemindersService);
  });

  afterEach(() => {
    controller.verify();
    TestBed.resetTestingModule();
  });

  it('POST /api/reminders/process', () => {
    const expected = {
      batchId: 'batch-1',
      totalCandidates: 4,
      succeeded: 4,
      failed: 0,
      skipped: 0,
      needsReview: 0,
      results: [],
    };

    service.processReminders().subscribe((got) => {
      expect(got).toEqual(expected);
    });

    const req = controller.expectOne('http://test-api:5091/api/reminders/process');
    expect(req.request.method).toBe('POST');
    req.flush(expected);
  });

  it('GET /api/reminders/runs/{batchId}', () => {
    const batchId = 'batch-1';
    const expected = [
      {
        attemptId: 'attempt-1',
        invoiceId: 'FAC-001',
        previousState: 'primerrecordatorio',
        nextState: 'segundorecordatorio',
        status: 'Completed',
        startedAt: '2026-10-21T12:00:00Z',
        smtpAcceptedAt: '2026-10-21T12:00:01Z',
        finishedAt: '2026-10-21T12:00:02Z',
        messageId: 'msg-1@example.test',
        failureCode: null,
        details: null,
      },
    ];

    service.getRun(batchId).subscribe((got) => {
      expect(got).toEqual(expected);
    });

    const req = controller.expectOne(
      `http://test-api:5091/api/reminders/runs/${encodeURIComponent(batchId)}`,
    );
    expect(req.request.method).toBe('GET');
    req.flush(expected);
  });

  it('normalizes numeric audit statuses returned by the API', () => {
    service.getRun('batch-numeric').subscribe((got) => {
      expect(got[0].status).toBe('Completed');
    });

    const req = controller.expectOne('http://test-api:5091/api/reminders/runs/batch-numeric');
    req.flush([
      {
        attemptId: 'attempt-1',
        invoiceId: 'FAC-001',
        previousState: 'primerrecordatorio',
        nextState: 'segundorecordatorio',
        status: 3,
        startedAt: null,
        smtpAcceptedAt: null,
        finishedAt: null,
        messageId: null,
        failureCode: null,
      },
    ]);
  });
});
