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
