import { Component, OnInit, inject, input, signal, computed } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormArray } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly scheduleService = inject(ScheduleService);
  private readonly bookingService = inject(BookingService);

  readonly schedule = signal<Schedule | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedSeats = signal<number[]>([]);

  // Display-only fields passed as query params from search results
  readonly displaySource = signal('');
  readonly displayDestination = signal('');
  readonly displayBusName = signal('');
  readonly displayBusNumber = signal('');
  readonly displayDepartureTime = signal('');
  readonly displayArrivalTime = signal('');
  readonly displayTravelDate = signal('');
  readonly displayMinPrice = signal<number | null>(null);

  readonly bookedSeatNumbers = computed<number[]>(() => {
    const s = this.schedule();
    if (!s || !s.totalSeats) return [];
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
    const qp = this.activatedRoute.snapshot.queryParams;
    this.displaySource.set(qp['source'] ?? '');
    this.displayDestination.set(qp['destination'] ?? '');
    this.displayBusName.set(qp['busName'] ?? '');
    this.displayBusNumber.set(qp['busNumber'] ?? '');
    this.displayDepartureTime.set(qp['departureTime'] ?? '');
    this.displayArrivalTime.set(qp['arrivalTime'] ?? '');
    this.displayTravelDate.set(qp['travelDate'] ?? '');
    this.displayMinPrice.set(qp['minSeatPrice'] ? +qp['minSeatPrice'] : null);

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
        seats: passengers,
      });
      await this.router.navigate(['/payment', bookingId], {
        queryParams: {
          source: this.displaySource(),
          destination: this.displayDestination(),
          busName: this.displayBusName(),
          busNumber: this.displayBusNumber(),
          departureTime: this.displayDepartureTime(),
          arrivalTime: this.displayArrivalTime(),
          travelDate: this.displayTravelDate(),
          minSeatPrice: this.displayMinPrice() ?? '',
          seatCount: this.selectedSeats().length,
        },
      });
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }

  formatTime(t: string): string {
    if (!t) return '';
    if (t.includes('T') || t.length > 8) {
      return new Date(t).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
    }
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  formatDate(d: string): string {
    if (!d) return '';
    return new Date(d + 'T00:00:00').toLocaleDateString('en-IN', {
      weekday: 'short', day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  duration(dep: string, arr: string): string {
    if (!dep || !arr) return '';
    const [dh, dm] = dep.split(':').map(Number);
    const [ah, am] = arr.split(':').map(Number);
    let depMins = dh * 60 + dm;
    let arrMins = ah * 60 + am;
    if (arrMins <= depMins) arrMins += 24 * 60;
    const diff = arrMins - depMins;
    const h = Math.floor(diff / 60);
    const m = diff % 60;
    return m > 0 ? `${h}h ${m}m` : `${h}h`;
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
