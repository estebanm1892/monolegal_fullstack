import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { DashboardService } from './features/dashboard/dashboard.service';
import { appConfig } from './app.config';

describe('App', () => {
  it('creates the application with the dashboard', async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [...appConfig.providers, provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;

    expect(page.querySelector('app-dashboard')).toBeTruthy();
  });

  it('resolves DashboardService from the real application providers', () => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });

    expect(TestBed.inject(DashboardService)).toBeTruthy();
  });
});
