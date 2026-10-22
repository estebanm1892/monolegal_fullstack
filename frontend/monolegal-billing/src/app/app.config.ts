import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { CustomersService } from './core/api/customers.service';
import { InvoicesService } from './core/api/invoices.service';
import { RemindersService } from './core/api/reminders.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withFetch()),
    CustomersService,
    InvoicesService,
    RemindersService,
  ],
};
