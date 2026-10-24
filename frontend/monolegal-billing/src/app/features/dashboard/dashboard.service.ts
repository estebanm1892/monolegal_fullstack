import { computed, Injectable, signal } from '@angular/core';
import { CustomersService } from '../../core/api/customers.service';
import { InvoicesService, InvoiceQuery } from '../../core/api/invoices.service';
import {
  RemindersService,
  ProcessRemindersResult,
  ReminderRunItem,
} from '../../core/api/reminders.service';
import { CustomerDto } from '../../core/models/customer.model';
import { InvoiceDto } from '../../core/models/invoice.model';
import { InvoiceSummaryDto } from '../../core/models/summary.model';

export interface ProcessingResultRow {
  readonly invoiceId: string;
  readonly invoiceNumber: string | null;
  readonly customerName: string | null;
  readonly previousState: string | null;
  readonly nextState: string | null;
  readonly status: string;
  readonly failureCode: string | null;
  readonly details: string | null;
}

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
  readonly customersLoading = signal(false);
  readonly invoicesLoading = signal(false);
  readonly summaryLoading = signal(false);
  readonly runDetailsLoading = signal(false);
  readonly customersError = signal<string | null>(null);
  readonly invoicesError = signal<string | null>(null);
  readonly summaryError = signal<string | null>(null);
  readonly processingError = signal<string | null>(null);
  readonly runDetailsError = signal<string | null>(null);
  readonly loading = computed(
    () => this.customersLoading() || this.invoicesLoading() || this.summaryLoading(),
  );
  readonly error = computed(
    () =>
      this.customersError() ??
      this.invoicesError() ??
      this.summaryError() ??
      this.processingError(),
  );
  readonly processing = signal(false);
  readonly result = signal<ProcessRemindersResult | null>(null);
  readonly runDetails = signal<readonly ReminderRunItem[]>([]);
  readonly resultInvoices = signal<readonly InvoiceDto[]>([]);
  readonly executionRows = computed<readonly ProcessingResultRow[]>(() => {
    const result = this.result();
    if (!result) {
      return [];
    }

    const auditByInvoice = new Map(
      this.runDetails().map((attempt) => [attempt.invoiceId, attempt]),
    );
    const invoicesById = new Map(this.resultInvoices().map((invoice) => [invoice.id, invoice]));
    const customersById = new Map(this.customers().map((customer) => [customer.id, customer]));

    return result.results.map((postResult) => {
      const audit = auditByInvoice.get(postResult.invoiceId);
      const invoice = invoicesById.get(postResult.invoiceId);
      const customer = invoice ? customersById.get(invoice.customerId) : null;

      return {
        invoiceId: postResult.invoiceId,
        invoiceNumber: invoice?.number ?? null,
        customerName: customer?.name ?? null,
        previousState: postResult.status === 'Skipped' ? null : (audit?.previousState ?? null),
        nextState:
          postResult.status === 'Succeeded' && audit?.status === 'Completed'
            ? audit.nextState
            : null,
        status: postResult.status,
        failureCode: postResult.failureCode ?? audit?.failureCode ?? null,
        details: postResult.details ?? audit?.details ?? null,
      };
    });
  });
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalCount = signal(0);

  private currentQuery: InvoiceQuery = {
    page: 1,
    pageSize: 10,
    customerId: null,
    status: null,
  };
  private invoiceRequestId = 0;
  private executionRequestId = 0;

  loadCustomers(): void {
    this.customersLoading.set(true);
    this.customersError.set(null);
    this.customersService.getCustomers().subscribe({
      next: (customers) => {
        this.customers.set(customers);
        this.customersLoading.set(false);
      },
      error: (err) => {
        this.customersError.set('No se pudieron cargar los clientes.');
        this.customersLoading.set(false);
      },
    });
  }

  loadInvoices(query?: Partial<InvoiceQuery>): void {
    if (query) {
      this.currentQuery = { ...this.currentQuery, ...query };
    }
    const requestId = ++this.invoiceRequestId;
    this.invoicesLoading.set(true);
    this.invoicesError.set(null);
    this.invoicesService.getInvoices(this.currentQuery).subscribe({
      next: (page) => {
        if (requestId !== this.invoiceRequestId) {
          return;
        }
        if (page.totalPages > 0 && this.currentQuery.page > page.totalPages) {
          this.currentQuery = { ...this.currentQuery, page: page.totalPages };
          this.loadInvoices();
          return;
        }
        this.invoices.set(page.items);
        this.page.set(page.page);
        this.totalPages.set(page.totalPages);
        this.totalCount.set(page.totalCount);
        this.invoicesLoading.set(false);
      },
      error: () => {
        if (requestId !== this.invoiceRequestId) {
          return;
        }
        this.invoicesError.set('No se pudieron cargar las facturas.');
        this.invoicesLoading.set(false);
      },
    });
  }

  loadSummary(): void {
    this.summaryLoading.set(true);
    this.summaryError.set(null);
    this.invoicesService.getSummary().subscribe({
      next: (summary) => {
        this.summary.set(summary);
        this.summaryLoading.set(false);
      },
      error: () => {
        this.summaryError.set('No se pudo cargar el resumen global.');
        this.summaryLoading.set(false);
      },
    });
  }

  processReminders(): void {
    if (this.processing()) {
      return;
    }
    this.processing.set(true);
    const executionRequestId = ++this.executionRequestId;
    this.processingError.set(null);
    this.runDetailsError.set(null);
    this.runDetailsLoading.set(false);
    this.result.set(null);
    this.runDetails.set([]);
    this.resultInvoices.set([]);

    this.remindersService.processReminders().subscribe({
      next: (result) => {
        this.result.set(result);
        this.resultInvoices.set(this.invoices());
        this.processing.set(false);
        if (result.results.length > 0) {
          this.loadRunDetails(result.batchId, executionRequestId);
          this.loadResultInvoiceMetadata(executionRequestId);
        } else {
          this.runDetailsLoading.set(false);
        }
        this.refreshReadModels();
      },
      error: () => {
        this.processingError.set(
          'No se pudo confirmar el resultado del procesamiento. Los datos se actualizaron para mostrar el estado conocido más reciente. No vuelvas a procesar hasta verificar el lote.',
        );
        this.processing.set(false);
        this.refreshReadModels();
      },
    });
  }

  setFilter(field: keyof InvoiceQuery, value: string | null): void {
    this.currentQuery = { ...this.currentQuery, [field]: value, page: 1 };
    this.loadInvoices();
  }

  clearFilters(): void {
    this.currentQuery = {
      ...this.currentQuery,
      customerId: null,
      status: null,
      page: 1,
    };
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

  private loadRunDetails(batchId: string, executionRequestId: number): void {
    this.runDetailsLoading.set(true);
    this.runDetailsError.set(null);
    this.remindersService.getRun(batchId).subscribe({
      next: (details) => {
        if (executionRequestId !== this.executionRequestId) {
          return;
        }
        this.runDetails.set(details);
        this.runDetailsLoading.set(false);
      },
      error: () => {
        if (executionRequestId !== this.executionRequestId) {
          return;
        }
        this.runDetailsError.set(
          'El lote terminó, pero no fue posible cargar el detalle de cada factura.',
        );
        this.runDetailsLoading.set(false);
      },
    });
  }

  private loadResultInvoiceMetadata(executionRequestId: number): void {
    this.invoicesService
      .getInvoices({
        page: 1,
        pageSize: 100,
        customerId: null,
        status: null,
      })
      .subscribe({
        next: (page) => {
          if (executionRequestId === this.executionRequestId) {
            this.resultInvoices.set(page.items);
          }
        },
        error: () => {
          if (executionRequestId === this.executionRequestId) {
            this.resultInvoices.set([]);
          }
        },
      });
  }

  private refreshReadModels(): void {
    this.loadInvoices();
    this.loadSummary();
  }
}
