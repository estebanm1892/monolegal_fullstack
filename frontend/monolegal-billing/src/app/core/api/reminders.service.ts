import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG } from './api-config.token';
import { ApiConfig } from './api-config';
import { ReminderProcessResponseDto } from '../models/reminder-result.model';

@Injectable()
export class RemindersService {
  constructor(
    private readonly http: HttpClient,
    @Optional() @Inject(API_CONFIG) private readonly apiConfig: ApiConfig,
  ) {
    if (!this.apiConfig) {
      throw new Error('API_CONFIG is required for RemindersService');
    }
  }

  processReminders(): Observable<ProcessRemindersResult> {
    return this.http.post<ProcessRemindersResult>(
      `${this.apiConfig.baseUrl}/api/reminders/process`,
      null,
    );
  }

  getRun(batchId: string): Observable<readonly {
    readonly attemptId: string;
    readonly invoiceId: string;
    readonly previousState: string;
    readonly nextState: string;
    readonly status: string;
    readonly startedAt: string | null;
    readonly smtpAcceptedAt: string | null;
    readonly finishedAt: string | null;
    readonly messageId: string | null;
    readonly failureCode: string | null;
  }[]> {
    return this.http.get<readonly {
      readonly attemptId: string;
      readonly invoiceId: string;
      readonly previousState: string;
      readonly nextState: string;
      readonly status: string;
      readonly startedAt: string | null;
      readonly smtpAcceptedAt: string | null;
      readonly finishedAt: string | null;
      readonly messageId: string | null;
      readonly failureCode: string | null;
    }[]>(`${this.apiConfig.baseUrl}/api/reminders/runs/${encodeURIComponent(batchId)}`);
  }
}

export interface ProcessRemindersResult {
  readonly batchId: string;
  readonly totalCandidates: number;
  readonly succeeded: number;
  readonly failed: number;
  readonly skipped: number;
  readonly needsReview: number;
  readonly results: readonly {
    readonly invoiceId: string;
    readonly status: string;
    readonly attemptId: string | null;
    readonly failureCode: string | null;
    readonly details: string | null;
  }[];
}
