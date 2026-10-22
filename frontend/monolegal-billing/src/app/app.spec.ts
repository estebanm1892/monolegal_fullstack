import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { CustomersService } from './core/api/customers.service';
import { InvoicesService } from './core/api/invoices.service';
import { RemindersService } from './core/api/reminders.service';
import { DashboardService } from './features/dashboard/dashboard.service';
import { API_CONFIG } from './core/api/api-config.token';

describe('App', () => {
  it('creates the application with the dashboard', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        CustomersService,
        InvoicesService,
        RemindersService,
        DashboardService,
        { provide: API_CONFIG, useValue: { baseUrl: 'http://test' } },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('app-dashboard')).toBeTruthy();
  });
});
