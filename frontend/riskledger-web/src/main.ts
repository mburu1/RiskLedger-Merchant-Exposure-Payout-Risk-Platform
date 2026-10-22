import { bootstrapApplication } from '@angular/platform-browser';
import { Component } from '@angular/core';
@Component({ selector: 'rl-root', standalone: true, template: `
<main class="shell"><header class="topbar"><a class="brand" href="/"><span class="mark">R</span> RiskLedger</a><span class="badge">SIMULATION ENVIRONMENT</span></header>
<section class="hero"><p class="eyebrow">MERCHANT EXPOSURE & PAYOUT RISK</p><h1>See the exposure.<br><span>Understand the decision.</span></h1><p class="intro">A financial operations workspace for future-dated obligations, reserves, risk decisions and simulated payouts.</p><aside class="notice"><strong>Portfolio simulation</strong><span>No real money, live payment rails or production underwriting decisions are involved.</span></aside></section>
<section class="metrics" aria-label="Platform overview"><article><label>Gross exposure</label><strong>—</strong><small>Waiting for API data</small></article><article><label>Reserve coverage</label><strong>—</strong><small>Not yet calculated</small></article><article><label>Payout eligibility</label><strong>—</strong><small>Not yet assessed</small></article></section>
<section class="panel"><div><p class="eyebrow">ENGINEERING STATUS</p><h2>Foundation online, workflows in progress</h2><p>The initial API includes health probes and a deterministic in-memory ledger demonstration. Durable ledger storage and risk workflows are planned next.</p></div><a class="button" href="http://localhost:5080/health/ready" target="_blank" rel="noreferrer">Check API readiness ↗</a></section><footer>RiskLedger · Synthetic data only · Financial correctness before features</footer></main>
`, styleUrl: './styles.css' })
class AppComponent {}
bootstrapApplication(AppComponent).catch((error: unknown) => console.error('Application bootstrap failed.', error));
