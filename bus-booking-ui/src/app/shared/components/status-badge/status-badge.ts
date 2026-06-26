import { Component, input } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="cssClass()">{{ label() }}</span>`,
  styles: [`
    .badge {
      display: inline-block;
      padding: 0.2rem 0.6rem;
      border-radius: 12px;
      font-size: 0.78rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }
    .confirmed { background: #dcfce7; color: #15803d; }
    .pending   { background: #fef9c3; color: #854d0e; }
    .cancelled { background: #fee2e2; color: #b91c1c; }
    .failed    { background: #fee2e2; color: #b91c1c; }
    .payment-pending { background: #e0f2fe; color: #0369a1; }
    .approved  { background: #dcfce7; color: #15803d; }
    .rejected  { background: #fee2e2; color: #b91c1c; }
    .inactive  { background: #f1f5f9; color: #64748b; }
    .active    { background: #dcfce7; color: #15803d; }
    .completed { background: #f0fdf4; color: #166534; }
    .default   { background: #f1f5f9; color: #475569; }
  `],
})
export class StatusBadgeComponent {
  readonly status = input.required<string>();

  label() { return this.status().replace(/([A-Z])/g, ' $1').trim(); }

  cssClass() {
    const s = this.status().toLowerCase();
    return s === 'paymentpending' ? 'payment-pending' : (s || 'default');
  }
}
