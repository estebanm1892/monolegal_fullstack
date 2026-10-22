import { Injectable, signal } from '@angular/core';
import { CustomersService } from '../../core/api/customers.service';
import {
  InvoicesService,
  InvoiceQuery,
} from '../../core/api/invoices.service';
import {
  RemindersService,
  ProcessRemindersResult,
} from '../../core/api/reminders.service';
import { CustomerDto } from '../../core/models/customer.model';
import { InvoiceDto } from '../../core/models/invoice.model';
import { InvoiceSummaryDto } from '../../core/models/summary.model';

@Injectable()
export class DashboardService {
  constructor(
    private readonly customersService: CustomersService,
    private readonly invoicesService: InvoicesService,
    private readonly remindersService: RemindersService,
  ) {}

  readonly customers = signal<CustomerDto[]>([]);
  readonly invoices = signal<InvoiceDto[]>([]);
  readonly summary = signal<InvoiceSummaryDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly processing = signal(false);
  readonly result = signal<ProcessRemindersResult | null>(null);
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);

  private currentQuery: InvoiceQuery = {
    page: 1,
    pageSize: 10,
    customerId: null,
    status: null,
  };

  loadCustomers(): void {
    this.loading.set(true);
    this.error.set(null);
    this.customersService.getCustomers().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'No se pudieron cargar los clientes');
        this.loading.set(false);
      },
    });
  }

  loadInvoices(query?: Partial<InvoiceQuery>): void {
    if (query) {
      this.currentQuery = { ...this.currentQuery, ...query };
    }
    this.loading.set(true);
    this.error.set(null);
    this.invoicesService.getInvoices(this.currentQuery).subscribe({
      next: (page) => {
        this.invoices.set(page.items);
        this.page.set(page.page);
        this.totalPages.set(page.totalPages);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.message || 'No se pudieron cargar las facturas');
        this.loading.set(false);
      },
    });
  }

  loadSummary(): void {
    this.invoicesService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
      },
      error: (err) => {
        this.error.set(err.message || 'No se pudo cargar el resumen');
      },
    });
  }

  processReminders(): void {
    if (this.processing()) {
      return;
    }
    this.processing.set(true);
    this.error.set(null);
    this.result.set(null);

    this.remindersService.processReminders().subscribe({
      next: (result) => {
        this.result.set(result);
        this.processing.set(false);
        // Refresh the dashboard so counts, states and rows reflect the batch.
        this.loadInvoices();
        this.loadSummary();
      },
      error: (err) => {
        this.error.set(
          err.message || 'No se pudo procesar el lote de recordatorios',
        );
        this.processing.set(false);
      },
    });
  }

  setFilter(field: keyof InvoiceQuery, value: string | null): void {
    this.currentQuery = { ...this.currentQuery, [field]: value, page: 1 };
    this.loadInvoices();
  }

  setPage(page: number): void {
    const lastPage = this.totalPages();
    if (page < 1 || (lastPage > 0 && page > lastPage)) {
      return;
    }
    this.currentQuery = { ...this.currentQuery, page };
    this.loadInvoices();
  }
}
