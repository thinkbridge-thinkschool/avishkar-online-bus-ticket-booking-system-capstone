export type ScheduleStatus = 'Active' | 'Cancelled' | 'Completed';

export interface Schedule {
  scheduleId: string;
  routeId: string;
  busId: string;
  vendorId?: string;
  fromCityName?: string;
  toCityName?: string;
  departureTime: string;
  arrivalTime: string;
  pricePerSeat: number;
  availableSeats: number;
  totalSeats: number;
  status: ScheduleStatus;
  busType?: string;
}

export interface SearchSchedulesRequest {
  fromCityId: string;
  toCityId: string;
  travelDate: string;
}

export interface CreateScheduleRequest {
  routeId: string;
  busId: string;
  departureTime: string;
  arrivalTime: string;
  pricePerSeat: number;
}

export interface UpdateScheduleRequest {
  departureTime?: string;
  arrivalTime?: string;
  pricePerSeat?: number;
  status?: ScheduleStatus;
}
