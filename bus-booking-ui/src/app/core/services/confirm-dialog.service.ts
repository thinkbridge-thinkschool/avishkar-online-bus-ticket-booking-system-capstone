import { Injectable, signal } from '@angular/core';

export interface ConfirmDialogOptions {
  title?: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  /** true renders the confirm button with danger (red) styling instead of primary. */
  danger?: boolean;
}

export interface ConfirmDialogState {
  open: boolean;
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  danger: boolean;
}

const CLOSED_STATE: ConfirmDialogState = {
  open: false,
  title: 'Confirm',
  message: '',
  confirmText: 'Confirm',
  cancelText: 'Cancel',
  danger: false,
};

// Singleton-backed replacement for window.confirm(). One <app-confirm-dialog/> is
// mounted at the app root (see app.html); any component can inject this service and
// `await confirmDialog.confirm({...})` instead of calling the native browser dialog.
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  readonly state = signal<ConfirmDialogState>(CLOSED_STATE);

  private pendingResolve: ((result: boolean) => void) | null = null;

  confirm(options: ConfirmDialogOptions): Promise<boolean> {
    // Only one confirmation can be in flight at a time — resolve any stale one as
    // cancelled so its awaiting caller doesn't hang forever.
    this.pendingResolve?.(false);

    this.state.set({
      open: true,
      title: options.title ?? 'Confirm',
      message: options.message,
      confirmText: options.confirmText ?? 'Confirm',
      cancelText: options.cancelText ?? 'Cancel',
      danger: options.danger ?? false,
    });

    return new Promise<boolean>(resolve => {
      this.pendingResolve = resolve;
    });
  }

  resolve(result: boolean): void {
    this.state.set(CLOSED_STATE);
    this.pendingResolve?.(result);
    this.pendingResolve = null;
  }
}
