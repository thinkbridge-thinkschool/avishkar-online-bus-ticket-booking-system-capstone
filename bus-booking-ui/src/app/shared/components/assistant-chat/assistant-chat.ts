import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AssistantService } from '../../../core/services/assistant.service';
import { AssistantUiService } from '../../../core/services/assistant-ui.service';
import { BookingService } from '../../../core/services/booking.service';
import { ConfirmDialogService } from '../../../core/services/confirm-dialog.service';
import type {
  AssistantBookingResult,
  AssistantCancelSuggestion,
  AssistantChatMessage,
  AssistantHistoryMessage,
  AssistantScheduleResult,
  AssistantVendorBus,
  AssistantVendorSchedule,
} from '../../../shared/models/assistant.model';

@Component({
  selector: 'app-assistant-chat',
  imports: [FormsModule, RouterLink],
  templateUrl: './assistant-chat.html',
  styleUrl: './assistant-chat.css',
})
export class AssistantChatComponent {
  private readonly assistant = inject(AssistantService);
  private readonly bookingService = inject(BookingService);
  private readonly confirmDialog = inject(ConfirmDialogService);
  private readonly scrollAnchor = viewChild<ElementRef<HTMLDivElement>>('scrollAnchor');

  readonly ui = inject(AssistantUiService);
  readonly open = this.ui.open;
  readonly messages = signal<AssistantChatMessage[]>([]);
  readonly input = signal('');
  readonly sending = signal(false);

  constructor() {
    // Auto-scroll to the newest message whenever the list changes.
    effect(() => {
      this.messages();
      this.sending();
      queueMicrotask(() => this.scrollAnchor()?.nativeElement.scrollIntoView({ behavior: 'smooth' }));
    });

    // Home's "Ask AI" affordance opens this same widget with a starting message pre-filled.
    effect(() => {
      if (this.open()) {
        const prefill = this.ui.consumePrefill();
        if (prefill) this.input.set(prefill);
      }
    });
  }

  toggle(): void {
    this.ui.toggle();
  }

  async send(): Promise<void> {
    const text = this.input().trim();
    if (!text || this.sending()) return;

    const history: AssistantHistoryMessage[] = this.messages()
      .filter(m => !m.isError)
      .map(m => ({ role: m.role === 'assistant' ? 'model' : 'user', text: m.text }));

    this.messages.update(list => [...list, { role: 'user', text }]);
    this.input.set('');
    this.sending.set(true);

    try {
      const response = await this.assistant.chat(text, history);
      this.messages.update(list => [
        ...list,
        { role: 'assistant', text: response.reply, toolResults: response.toolResults },
      ]);
    } catch (err: unknown) {
      const message = (err as Error).message || 'The assistant is temporarily unavailable. Please try again, or check the FAQ section on the home page.';
      this.messages.update(list => [...list, { role: 'assistant', text: message, isError: true }]);
    } finally {
      this.sending.set(false);
    }
  }

  parseSchedules(dataJson: string): AssistantScheduleResult[] {
    return this.tryParse<AssistantScheduleResult[]>(dataJson) ?? [];
  }

  parseVendorBuses(dataJson: string): AssistantVendorBus[] {
    return this.tryParse<AssistantVendorBus[]>(dataJson) ?? [];
  }

  parseVendorSchedules(dataJson: string): AssistantVendorSchedule[] {
    return this.tryParse<AssistantVendorSchedule[]>(dataJson) ?? [];
  }

  parseBookingsList(dataJson: string): AssistantBookingResult[] {
    return this.tryParse<AssistantBookingResult[]>(dataJson) ?? [];
  }

  parseBooking(dataJson: string): AssistantBookingResult | null {
    return this.tryParse<AssistantBookingResult>(dataJson);
  }

  parseCancelSuggestion(dataJson: string): AssistantCancelSuggestion | null {
    return this.tryParse<AssistantCancelSuggestion>(dataJson);
  }

  // The AI never cancels a booking itself — this goes through the exact same confirm dialog
  // and cancel endpoint as the My Bookings page. See suggest_cancel_booking on the backend.
  async confirmCancel(bookingId: string): Promise<void> {
    const confirmed = await this.confirmDialog.confirm({
      title: 'Confirm Cancellation',
      message: 'Are you sure you want to cancel this booking?',
      confirmText: 'Cancel Booking',
      danger: true,
    });
    if (!confirmed) return;

    try {
      await this.bookingService.cancelBooking(bookingId);
      this.messages.update(list => [
        ...list,
        { role: 'assistant', text: `Done — booking ${bookingId} has been cancelled.` },
      ]);
    } catch (err: unknown) {
      this.messages.update(list => [...list, { role: 'assistant', text: (err as Error).message, isError: true }]);
    }
  }

  formatTime(t: string | undefined): string {
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  private tryParse<T>(json: string): T | null {
    try {
      return JSON.parse(json) as T;
    } catch {
      return null;
    }
  }
}
