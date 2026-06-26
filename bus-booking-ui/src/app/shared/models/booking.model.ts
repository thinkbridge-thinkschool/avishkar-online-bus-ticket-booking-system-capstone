export type BookingStatus =
  | 'Pending'
  | 'PaymentPending'
  | 'Confirmed'
  | 'Cancelled'
  | 'PaymentFailed';

export interface BookedSeat {
  seatNumber: number;
  passengerName: string;
  passengerAge: number;
  seatPrice: number;
  passengerGender?: string;
}

export interface Booking {
  bookingId: string;
  scheduleId: string;
  status: BookingStatus;
  totalAmount: number;
  bookedAt: string;
  seats: BookedSeat[];
  fromCityName?: string;
  toCityName?: string;
  departureTime?: string;
}

export interface CreateBookingRequest {
  scheduleId: string;
  seats: {
    seatNumber: number;
    passengerName: string;
    passengerAge: number;
    passengerGender: string;
    passengerPhone?: string;
    passengerEmail?: string;
  }[];
}
