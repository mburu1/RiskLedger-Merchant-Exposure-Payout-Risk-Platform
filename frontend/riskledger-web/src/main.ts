import { provideHttpClient, HttpClient } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, RouterLink, RouterLinkActive, RouterOutlet, Routes } from '@angular/router';
import { Component, Injectable, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { catchError, of, timeout } from 'rxjs';

declare global {
  interface Window { __RISKLEDGER_API_URL__?: string; }
}

interface HealthResponse {
  status: 'Healthy' | 'Degraded' | 'Unhealthy';
  totalDuration?: string;
  entries?: Array<{ name: string; status: string; description?: string }>;
}

@Injectable({ providedIn: 'root' })
class ServiceHealthClient {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = (window.__RISKLEDGER_API_URL__ ?? 'http://localhost:5080').replace(/\/$/, '');

  getReadiness() {
    return this.http.get<HealthResponse>(`${this.baseUrl}/health/ready`).pipe(
      timeout(5000),
      catchError(() => of(null))
    );
  }
}

@Component({
  selector: 'rl-health',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="panel health-panel">
      <div><p class="eyebrow">SERVICE HEALTH</p><h2>API and database readiness</h2>
        <p>Readiness requires the configured PostgreSQL database to accept connections.</p></div>
      <div class="health-result" aria-live="polite">
        <span class="status-dot" [class.online]="health?.status === 'Healthy'" [class.offline]="health === null"></span>
        <strong>{{ health?.status ?? 'Unavailable' }}</strong>
        <button class="button secondary" type="button" (click)="refresh()">Refresh status</button>
      </div>
    </section>
    <p class="hint" *ngIf="health === null">The API could not be reached. Start PostgreSQL and the .NET API, then refresh.</p>
    <p class="hint" *ngIf="health?.status === 'Healthy'">The API readiness probe reports all required dependencies as healthy.</p>
  `,
})
class HealthComponent {
  private readonly client = inject(ServiceHealthClient);
  health: HealthResponse | null = null;
  constructor() { this.refresh(); }
  refresh(): void { this.client.getReadiness().subscribe(result => this.health = result); }
}

@Component({
  selector: 'rl-home',
  standalone: true,
  imports: [RouterLink, HealthComponent],
  template: `
    <section class="hero">
      <p class="eyebrow">MERCHANT EXPOSURE & PAYOUT RISK</p>
      <h1>See the exposure.<br><span>Understand the decision.</span></h1>
      <p class="intro">A financial operations workspace for future-dated obligations, reserves, risk decisions and simulated payouts.</p>
      <aside class="notice"><strong>Portfolio simulation</strong><span>No real money, live payment rails or production underwriting decisions are involved.</span></aside>
    </section>
    <section class="metrics" aria-label="Platform overview">
      <article><label>Gross exposure</label><strong>—</strong><small>Waiting for domain data</small></article>
      <article><label>Reserve coverage</label><strong>—</strong><small>Not yet calculated</small></article>
      <article><label>Payout eligibility</label><strong>—</strong><small>Not yet assessed</small></article>
    </section>
    <rl-health></rl-health>
  `,
})
class HomeComponent {}

@Component({
  selector: 'rl-root',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <main class="shell">
      <header class="topbar">
        <a class="brand" routerLink="/"><span class="mark">R</span> RiskLedger</a>
        <nav aria-label="Main navigation"><a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Overview</a><a routerLink="/health" routerLinkActive="active">Service health</a></nav>
        <span class="badge">SIMULATION ENVIRONMENT</span>
      </header>
      <router-outlet></router-outlet>
      <footer>RiskLedger · Synthetic data only · Financial correctness before features</footer>
    </main>
  `,
})
class AppComponent {}

@Component({
  selector: 'rl-health-page',
  standalone: true,
  imports: [HealthComponent],
  template: `<section class="hero compact"><p class="eyebrow">OPERATIONS / HEALTH</p><h1>Service <span>health.</span></h1><p class="intro">Check whether the API and its PostgreSQL dependency are ready.</p></section><rl-health></rl-health>`,
})
class HealthPageComponent {}

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'health', component: HealthPageComponent },
  { path: '**', redirectTo: '' },
];

bootstrapApplication(AppComponent, {
  providers: [provideHttpClient(), provideRouter(routes)],
}).catch((error: unknown) => console.error('Application bootstrap failed.', error));
