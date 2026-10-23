import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { map, Observable } from 'rxjs';
import { API_CONFIG } from './api-config.token';
import { ApiConfig } from './api-config';
import {
  ReminderAttemptStatus,
  ReminderProcessResponseDto,
  ReminderRunItemDto,
} from '../models/reminder-result.model';

interface ReminderRunApiItem extends Omit<ReminderRunItemDto, 'status' | 'details'> {
  readonly status: number | string;
  readonly details?: string | null;
}

const attemptStatuses: readonly ReminderAttemptStatus[] = [
  'Claimed',
  'Dispatching',
  'SmtpAccepted',
  'Completed',
  'Failed',
  'NeedsReview',
];

function normalizeAttemptStatus(status: number | string): ReminderAttemptStatus {
  if (typeof status === 'number') {
    return attemptStatuses[status] ?? 'Unknown';
  }

  return attemptStatuses.includes(status as ReminderAttemptStatus)
    ? (status as ReminderAttemptStatus)
    : 'Unknown';
}

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

  processReminders(): Observable<ReminderProcessResponseDto> {
    return this.http.post<ReminderProcessResponseDto>(
      `${this.apiConfig.baseUrl}/api/reminders/process`,
      null,
    );
  }

  getRun(batchId: string): Observable<readonly ReminderRunItemDto[]> {
    return this.http
      .get<readonly ReminderRunApiItem[]>(
        `${this.apiConfig.baseUrl}/api/reminders/runs/${encodeURIComponent(batchId)}`,
      )
      .pipe(
        map((items) =>
          items.map((item) => ({
            ...item,
            status: normalizeAttemptStatus(item.status),
            details: item.details ?? null,
          })),
        ),
      );
  }
}

export type ProcessRemindersResult = ReminderProcessResponseDto;
export type ReminderRunItem = ReminderRunItemDto;
