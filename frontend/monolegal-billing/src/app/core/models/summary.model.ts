export interface InvoiceSummaryDto {
  readonly totalInvoices: number;
  readonly countByStatus: Record<string, number>;
  readonly countByCustomer: Record<string, number>;
  readonly totalAmountCop: number;
}
