import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    @if (dialog.state().open) {
      <div class="confirm-backdrop" (click)="onCancel()">
        <div
          class="confirm-modal"
          role="alertdialog"
          aria-modal="true"
          aria-labelledby="confirm-dialog-title"
          aria-describedby="confirm-dialog-message"
          (click)="$event.stopPropagation()"
          (keydown)="onKeydown($event)"
        >
          <h2 id="confirm-dialog-title">{{ dialog.state().title }}</h2>
          <p id="confirm-dialog-message">{{ dialog.state().message }}</p>
          <div class="confirm-actions">
            <button #cancelBtn type="button" class="btn btn-outline" (click)="onCancel()">
              {{ dialog.state().cancelText }}
            </button>
            <button
              #confirmBtn
              type="button"
              class="btn"
              [class.btn-danger]="dialog.state().danger"
              [class.btn-primary]="!dialog.state().danger"
              (click)="onConfirm()"
            >
              {{ dialog.state().confirmText }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .confirm-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(15, 23, 42, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
      animation: confirm-fade-in 0.15s ease-out;
    }
    .confirm-modal {
      background: #fff;
      border-radius: 12px;
      box-shadow: 0 20px 40px rgba(0, 0, 0, 0.2);
      width: 100%;
      max-width: 420px;
      padding: 1.75rem;
      animation: confirm-scale-in 0.15s ease-out;
    }
    .confirm-modal h2 {
      margin: 0 0 0.75rem;
      font-size: 1.15rem;
    }
    .confirm-modal p {
      margin: 0 0 1.5rem;
      color: #6b7280;
      font-size: 0.95rem;
      line-height: 1.5;
    }
    .confirm-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }
    @keyframes confirm-fade-in { from { opacity: 0; } to { opacity: 1; } }
    @keyframes confirm-scale-in { from { opacity: 0; transform: scale(0.96); } to { opacity: 1; transform: scale(1); } }
  `],
})
export class ConfirmDialogComponent {
  readonly dialog = inject(ConfirmDialogService);

  private readonly cancelBtn = viewChild<ElementRef<HTMLButtonElement>>('cancelBtn');
  private readonly confirmBtn = viewChild<ElementRef<HTMLButtonElement>>('confirmBtn');

  constructor() {
    effect(() => {
      if (this.dialog.state().open) {
        // Default focus to the non-destructive action, once the modal has painted.
        setTimeout(() => this.cancelBtn()?.nativeElement.focus(), 0);
      }
    });
  }

  onCancel(): void {
    this.dialog.resolve(false);
  }

  onConfirm(): void {
    this.dialog.resolve(true);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      event.preventDefault();
      this.onCancel();
      return;
    }
    if (event.key !== 'Tab') return;

    const cancel = this.cancelBtn()?.nativeElement;
    const confirmEl = this.confirmBtn()?.nativeElement;
    if (!cancel || !confirmEl) return;

    // Only two focusable elements exist inside the dialog — trap Tab between them.
    if (event.shiftKey && document.activeElement === cancel) {
      event.preventDefault();
      confirmEl.focus();
    } else if (!event.shiftKey && document.activeElement === confirmEl) {
      event.preventDefault();
      cancel.focus();
    }
  }
}
