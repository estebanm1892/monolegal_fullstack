export interface InvoiceDto {
  readonly id: string;
  readonly customerId: string;
  readonly number: string;
  readonly issuedAt: string;
  readonly dueAt: string;
  readonly updatedAt: string;
  readonly amount: number;
  readonly currency: string;
  readonly state: string;
}
