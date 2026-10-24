import { Component, ElementRef, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from './dashboard.service';
import { CustomerDto } from '../../core/models/customer.model';
import { InvoiceDto } from '../../core/models/invoice.model';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  readonly service = inject(DashboardService);

  readonly customers = this.service.customers;
  readonly invoices = this.service.invoices;
  readonly summary = this.service.summary;
  readonly loading = this.service.loading;
  readonly error = this.service.error;
  readonly processing = this.service.processing;
  readonly result = this.service.result;
  readonly page = this.service.page;
  readonly totalPages = this.service.totalPages;
  readonly totalCount = this.service.totalCount;
  readonly customersLoading = this.service.customersLoading;
  readonly invoicesLoading = this.service.invoicesLoading;
  readonly summaryLoading = this.service.summaryLoading;
  readonly runDetailsLoading = this.service.runDetailsLoading;
  readonly customersError = this.service.customersError;
  readonly invoicesError = this.service.invoicesError;
  readonly summaryError = this.service.summaryError;
  readonly processingError = this.service.processingError;
  readonly runDetailsError = this.service.runDetailsError;
  readonly executionRows = this.service.executionRows;
  readonly confirmationOpen = signal(false);

  selectedCustomer: string | null = null;
  selectedStatus: string | null = null;

  constructor() {
    this.service.loadCustomers();
    this.service.loadInvoices();
    this.service.loadSummary();
  }

  onCustomerChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const value = target ? target.value : '';
    this.selectedCustomer = value || null;
    this.service.setFilter('customerId', this.selectedCustomer);
  }

  onStatusChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const value = target ? target.value : '';
    this.selectedStatus = value || null;
    this.service.setFilter('status', this.selectedStatus);
  }

  onClearFilters(): void {
    this.selectedCustomer = null;
    this.selectedStatus = null;
    this.service.clearFilters();
  }

  onPage(page: number): void {
    this.service.setPage(page);
  }

  trackByInvoice(index: number, invoice: InvoiceDto): string {
    return invoice.id;
  }

  trackByCustomer(index: number, customer: CustomerDto): string {
    return customer.id;
  }

  getCustomerName(customerId: string): string {
    const customer = this.customers().find((c) => c.id === customerId);
    return customer ? customer.name : customerId;
  }

  trackByResult(index: number, item: { invoiceId: string }): string {
    return item.invoiceId;
  }

  processReminders(): void {
    this.confirmationOpen.set(true);
    queueMicrotask(() => {
      this.host.nativeElement
        .querySelector<HTMLButtonElement>('[data-testid="confirm-process-btn"]')
        ?.focus();
    });
  }

  cancelProcessing(): void {
    this.confirmationOpen.set(false);
    queueMicrotask(() => {
      this.host.nativeElement
        .querySelector<HTMLButtonElement>('[data-testid="process-btn"]')
        ?.focus();
    });
  }

  confirmProcessing(): void {
    this.confirmationOpen.set(false);
    this.service.processReminders();
  }

  expectedFirstTransitions(): number {
    return this.summary()?.countByStatus['primerrecordatorio'] ?? 0;
  }

  expectedSecondTransitions(): number {
    return this.summary()?.countByStatus['segundorecordatorio'] ?? 0;
  }

  expectedEmails(): number {
    return this.expectedFirstTransitions() + this.expectedSecondTransitions();
  }

  statusLabel(state: string): string {
    const labels: Record<string, string> = {
      primerrecordatorio: 'Primer recordatorio',
      segundorecordatorio: 'Segundo recordatorio',
      desactivado: 'Desactivada',
      pendiente: 'Pendiente',
    };
    return labels[state] ?? state;
  }

  formatMoney(value: number): string {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      minimumFractionDigits: 0,
    }).format(value);
  }

  // Date-only values (issued/due dates) are stored as UTC midnight, so they
  // must be rendered in UTC; converting to local time would shift a day back.
  formatDateOnly(value: string): string {
    return new Date(value).toLocaleDateString('es-CO', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      timeZone: 'UTC',
    });
  }

  // Timestamps (updatedAt) are real instants and are shown in local time.
  formatTimestamp(value: string): string {
    return new Date(value).toLocaleString('es-CO', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  hasNeedsReview(): boolean {
    return (this.result()?.needsReview ?? 0) > 0;
  }

  outcomeLabel(status: string): string {
    const labels: Record<string, string> = {
      Succeeded: 'Correo enviado y estado actualizado',
      Completed: 'Procesada correctamente',
      Failed: 'No se pudo procesar',
      Skipped: 'Omitida',
      NeedsReview: 'Requiere revisión manual',
      Claimed: 'En preparación',
      Dispatching: 'Envío iniciado',
      SmtpAccepted: 'Correo aceptado; actualización pendiente',
    };
    return labels[status] ?? 'Resultado no reconocido';
  }

  outcomeClass(status: string): string {
    return `outcome-${String(status).toLowerCase()}`;
  }

  resultStateLabel(state: string | null): string {
    return state ? this.statusLabel(state) : 'No disponible';
  }

  resultStateOutcome(status: string): string {
    switch (status) {
      case 'Failed':
        return 'Estado no actualizado por este intento';
      case 'NeedsReview':
        return 'Resultado no confirmado';
      case 'Skipped':
        return 'No procesada';
      default:
        return 'Detalle del estado no disponible';
    }
  }

  resultValue(value: string | null): string {
    return value ?? 'No disponible';
  }
}
