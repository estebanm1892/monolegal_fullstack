export interface ReminderInvoiceResultDto {
  readonly invoiceId: string;
  readonly status: string;
  readonly attemptId: string | null;
  readonly failureCode: string | null;
  readonly details: string | null;
}

export interface ReminderProcessResponseDto {
  readonly batchId: string;
  readonly totalCandidates: number;
  readonly succeeded: number;
  readonly failed: number;
  readonly skipped: number;
  readonly needsReview: number;
  readonly results: readonly ReminderInvoiceResultDto[];
}

export interface ReminderRunItemDto {
  readonly attemptId: string;
  readonly invoiceId: string;
  readonly previousState: string;
  readonly nextState: string;
  readonly status: ReminderAttemptStatus;
  readonly startedAt: string | null;
  readonly smtpAcceptedAt: string | null;
  readonly finishedAt: string | null;
  readonly messageId: string | null;
  readonly failureCode: string | null;
  readonly details: string | null;
}

export type ReminderAttemptStatus =
  'Claimed' | 'Dispatching' | 'SmtpAccepted' | 'Completed' | 'Failed' | 'NeedsReview' | 'Unknown';
