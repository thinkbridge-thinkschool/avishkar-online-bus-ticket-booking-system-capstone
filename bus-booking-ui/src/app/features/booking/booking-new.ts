import { Component, OnInit, inject, input, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { Router } from '@angular/router';
import { ScheduleService } from '../../core/services/schedule.service';
import { BookingService } from '../../core/services/booking.service';
import { SeatMapComponent } from '../../shared/components/seat-map/seat-map';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { Schedule } from '../../shared/models/schedule.model';

@Component({
  selector: 'app-booking-new',
  imports: [ReactiveFormsModule, SeatMapComponent, LoadingSpinnerComponent],
  templateUrl: './booking-new.html',
  styleUrl: './booking-new.css',
})
export class BookingNewComponent implements OnInit {
  readonly scheduleId = input.required<string>();
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly scheduleService = inject(ScheduleService);
  private readonly bookingService = inject(BookingService);

  readonly schedule = signal<Schedule | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedSeats = signal<number[]>([]);

  readonly bookedSeatNumbers = computed<number[]>(() => {
    const s = this.schedule();
    if (!s) return [];
    return Array.from(
      { length: (s.totalSeats - s.availableSeats) },
      (_, i) => i + 1
    );
  });

  readonly form = this.fb.nonNullable.group({
    passengers: this.fb.array([this.createPassengerGroup()]),
  });

  get passengersArray(): FormArray {
    return this.form.controls.passengers;
  }

  createPassengerGroup(): FormGroup {
    return this.fb.nonNullable.group({
      passengerName: ['', [Validators.required, Validators.minLength(2)]],
      passengerAge: [0, [Validators.required, Validators.min(1), Validators.max(120)]],
      passengerGender: ['', Validators.required],
      passengerPhone: [''],
      passengerEmail: ['', Validators.email],
    });
  }

  async ngOnInit(): Promise<void> {
    try {
      const s = await this.scheduleService.getById(this.scheduleId());
      this.schedule.set(s);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  onSeatsChange(seats: number[]): void {
    this.selectedSeats.set(seats);
    const current = this.passengersArray.length;
    const needed = seats.length;
    while (this.passengersArray.length < needed) {
      this.passengersArray.push(this.createPassengerGroup());
    }
    while (this.passengersArray.length > needed) {
      this.passengersArray.removeAt(this.passengersArray.length - 1);
    }
  }

  async submit(): Promise<void> {
    if (this.selectedSeats().length === 0) {
      this.error.set('Please select at least one seat.');
      return;
    }
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    this.submitting.set(true);
    this.error.set(null);
    try {
      interface PassengerFormValue {
        passengerName: string;
        passengerAge: number;
        passengerGender: string;
        passengerPhone: string;
        passengerEmail: string;
      }
      const values = this.form.getRawValue();
      const passengerValues = values.passengers as PassengerFormValue[];
      const passengers = passengerValues.map((p, i) => ({
        seatNumber: this.selectedSeats()[i],
        passengerName: p.passengerName,
        passengerAge: p.passengerAge,
        passengerGender: p.passengerGender,
        passengerPhone: p.passengerPhone || undefined,
        passengerEmail: p.passengerEmail || undefined,
      }));
      const bookingId = await this.bookingService.createBooking({
        scheduleId: this.scheduleId(),
        passengers,
      });
      await this.router.navigate(['/payment', bookingId]);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  fieldError(i: number, name: string): string | null {
    const ctrl = (this.passengersArray.at(i) as FormGroup).get(name);
    if (!ctrl?.invalid || !ctrl.touched) return null;
    if (ctrl.errors?.['required']) return `${name.replace(/([A-Z])/g, ' $1')} is required.`;
    if (ctrl.errors?.['min']) return 'Age must be at least 1.';
    if (ctrl.errors?.['max']) return 'Age must be at most 120.';
    if (ctrl.errors?.['email']) return 'Enter a valid email.';
    if (ctrl.errors?.['minlength']) return 'Name must be at least 2 characters.';
    return null;
  }
}
