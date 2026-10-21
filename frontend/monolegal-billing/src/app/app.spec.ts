import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  it('renders an accessible landing page with the current implementation scope', async () => {
    await TestBed.configureTestingModule({ imports: [App] }).compileComponents();

    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const page = fixture.nativeElement as HTMLElement;
    const main = page.querySelector('main');
    const heading = main?.querySelector('h1');

    expect(heading?.textContent).toBe('Monolegal Billing');
    expect(main?.getAttribute('aria-labelledby')).toBe(heading?.id);
    expect(main?.textContent).toContain('Phase 1: application foundation.');
    expect(main?.textContent).toContain(
      'Invoice viewing and manual reminders will be added in later phases.',
    );
  });
});
