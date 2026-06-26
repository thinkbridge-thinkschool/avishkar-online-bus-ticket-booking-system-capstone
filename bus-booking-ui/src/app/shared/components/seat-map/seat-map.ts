import { Component, computed, input, model } from '@angular/core';

export interface SeatSelection {
  seatNumber: number;
  selected: boolean;
}

@Component({
  selector: 'app-seat-map',
  templateUrl: './seat-map.html',
  styleUrl: './seat-map.css',
})
export class SeatMapComponent {
  readonly totalSeats = input.required<number>();
  readonly bookedSeats = input<number[]>([]);
  readonly maxSelectable = input<number>(4);
  readonly selected = model<number[]>([]);

  readonly seats = computed(() =>
    Array.from({ length: this.totalSeats() }, (_, i) => i + 1)
  );

  readonly rows = computed(() => {
    const seatsPerRow = 4;
    const all = this.seats();
    const result: number[][] = [];
    for (let i = 0; i < all.length; i += seatsPerRow) {
      result.push(all.slice(i, i + seatsPerRow));
    }
    return result;
  });

  isBooked(seat: number): boolean {
    return this.bookedSeats().includes(seat);
  }

  isSelected(seat: number): boolean {
    return this.selected().includes(seat);
  }

  toggle(seat: number): void {
    if (this.isBooked(seat)) return;
    const current = this.selected();
    if (current.includes(seat)) {
      this.selected.set(current.filter(s => s !== seat));
    } else if (current.length < this.maxSelectable()) {
      this.selected.set([...current, seat]);
    }
  }

  seatClass(seat: number): string {
    if (this.isBooked(seat)) return 'booked';
    if (this.isSelected(seat)) return 'selected';
    return 'available';
  }
}
