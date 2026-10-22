import { Component, inject } from '@angular/core';
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
    this.service.setFilter('customerId', null);
    this.service.setFilter('status', null);
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
    if (!window.confirm('¿Procesar los recordatorios pendientes ahora?')) {
      return;
    }
    this.service.processReminders();
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
    return new Date(value).toLocaleDateString('es-CO', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  }

  hasNeedsReview(): boolean {
    return (this.result()?.needsReview ?? 0) > 0;
  }
}
