import { Injectable, signal } from '@angular/core';

// Lets any component (e.g. the Home search card's "Ask AI" link) open the single, globally
// mounted AssistantChatComponent, instead of each place needing its own chat instance.
@Injectable({ providedIn: 'root' })
export class AssistantUiService {
  readonly open = signal(false);
  readonly prefill = signal<string | null>(null);

  toggle(): void {
    this.open.update(o => !o);
  }

  openWithPrefill(text: string): void {
    this.prefill.set(text);
    this.open.set(true);
  }

  consumePrefill(): string | null {
    const value = this.prefill();
    this.prefill.set(null);
    return value;
  }
}
