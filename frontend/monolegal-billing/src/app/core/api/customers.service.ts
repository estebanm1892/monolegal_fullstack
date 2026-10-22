import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, Optional } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG } from './api-config.token';
import { ApiConfig } from './api-config';
import { CustomerDto } from '../models/customer.model';

@Injectable()
export class CustomersService {
  constructor(
    private readonly http: HttpClient,
    @Optional() @Inject(API_CONFIG) private readonly apiConfig: ApiConfig,
  ) {
    if (!this.apiConfig) {
      throw new Error('API_CONFIG is required for CustomersService');
    }
  }

  getCustomers(): Observable<CustomerDto[]> {
    return this.http.get<CustomerDto[]>(`${this.apiConfig.baseUrl}/api/customers`);
  }
}
