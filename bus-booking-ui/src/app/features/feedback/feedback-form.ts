import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { FeedbackService } from '../../core/services/feedback.service';
import { BookingService } from '../../core/services/booking.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Booking } from '../../shared/models/booking.model';

@Component({
  selector: 'app-feedback-form',
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinnerComponent],
  templateUrl: './feedback-form.html',
  styleUrl: './feedback-form.css',
})
export class FeedbackFormComponent implements OnInit {
  readonly bookingId = input.required<string>();
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);
  private readonly feedbackService = inject(FeedbackService);

  readonly booking = signal<Booking | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitted = signal(false);

  readonly form = this.fb.nonNullable.group({
    rating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    comment: ['', Validators.maxLength(1000)],
  });

  async ngOnInit(): Promise<void> {
    try {
      this.booking.set(await this.bookingService.getById(this.bookingId()));
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const b = this.booking();
    if (!b) return;
    this.submitting.set(true);
    this.error.set(null);
    try {
      const { rating, comment } = this.form.getRawValue();
      await this.feedbackService.createFeedback({
        bookingId: this.bookingId(),
        scheduleId: b.scheduleId,
        rating,
        comment: comment || undefined,
      });
      this.submitted.set(true);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }

  setRating(n: number): void {
    this.form.controls.rating.setValue(n);
  }

  get ratingLabel(): string {
    const labels: Record<number, string> = {
      1: 'Poor', 2: 'Fair', 3: 'Good', 4: 'Very Good', 5: 'Excellent',
    };
    return labels[this.form.controls.rating.value] ?? '';
  }

  get commentLength(): number {
    return (this.form.controls.comment.value ?? '').length;
  }

  stars = [1, 2, 3, 4, 5];
}
