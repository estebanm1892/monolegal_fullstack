import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG } from './api-config.token';
import { ApiConfig } from './api-config';
import { InvoiceDto } from '../models/invoice.model';
import { InvoiceSummaryDto } from '../models/summary.model';

export interface InvoiceQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly customerId?: string | null;
  readonly status?: string | null;
}

@Injectable()
export class InvoicesService {
  constructor(
    private readonly http: HttpClient,
    @Optional() @Inject(API_CONFIG) private readonly apiConfig: ApiConfig,
  ) {
    if (!this.apiConfig) {
      throw new Error('API_CONFIG is required for InvoicesService');
    }
  }

  getInvoices(query: InvoiceQuery): Observable<{
    items: InvoiceDto[];
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  }> {
    const params = new URLSearchParams({
      page: String(query.page),
      pageSize: String(query.pageSize),
    });
    if (query.customerId) {
      params.set('customerId', query.customerId);
    }
    if (query.status) {
      params.set('status', query.status);
    }
    return this.http.get<{
      items: InvoiceDto[];
      page: number;
      pageSize: number;
      totalCount: number;
      totalPages: number;
    }>(`${this.apiConfig.baseUrl}/api/invoices?${params.toString()}`);
  }

  getSummary(): Observable<InvoiceSummaryDto> {
    return this.http.get<InvoiceSummaryDto>(`${this.apiConfig.baseUrl}/api/invoices/summary`);
  }
}
