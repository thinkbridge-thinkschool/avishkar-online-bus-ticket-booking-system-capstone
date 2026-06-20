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
  passengerGender: string;
  price: number;
  passengerPhone?: string;
  passengerEmail?: string;
}

export interface Booking {
  bookingId: string;
  userId: string;
  scheduleId: string;
  fromCityName?: string;
  toCityName?: string;
  departureTime?: string;
  bookedSeats: BookedSeat[];
  status: BookingStatus;
  totalAmount: number;
  createdAt: string;
}

export interface CreateBookingRequest {
  scheduleId: string;
  passengers: {
    seatNumber: number;
    passengerName: string;
    passengerAge: number;
    passengerGender: string;
    passengerPhone?: string;
    passengerEmail?: string;
  }[];
}
