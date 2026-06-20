import { Component } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  template: `
    <div class="loading-container" role="status" aria-live="polite">
      <div class="spinner" aria-hidden="true"></div>
      <span>Loading…</span>
    </div>
  `,
  styles: [`
    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 3rem;
      gap: 1rem;
      color: #6b7280;
    }
    .spinner {
      width: 36px;
      height: 36px;
      border: 3px solid #e2e8f0;
      border-top-color: #1a2b4a;
      border-radius: 50%;
      animation: spin 0.7s linear infinite;
    }
    @keyframes spin { to { transform: rotate(360deg); } }
  `],
})
export class LoadingSpinnerComponent {}
