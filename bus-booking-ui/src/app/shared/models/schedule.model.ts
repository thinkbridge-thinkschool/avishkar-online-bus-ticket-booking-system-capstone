// Fields returned by GET /api/v1/schedules/search (ScheduleSummaryDto)
// Fields returned by GET /api/v1/schedules/{id}  (ScheduleDetailDto)
// Fields returned by GET /api/v1/schedules/vendor (VendorScheduleDto)
// All optional except the guaranteed common fields so each component typechecks.
export interface Schedule {
  scheduleId: string;
  departureTime: string;   // "HH:mm:ss" from .NET TimeOnly
  arrivalTime: string;     // "HH:mm:ss" from .NET TimeOnly
  availableSeats: number;

  // ScheduleSummaryDto (search)
  busName?: string;
  busNumber?: string;
  busType?: string;        // "Seater" | "Sleeper" | "SemiSleeper"
  source?: string;
  destination?: string;
  travelDate?: string;
  minSeatPrice?: number | null;

  // ScheduleDetailDto / VendorScheduleDto
  busId?: string;
  routeId?: string;
  isActive?: boolean;
  totalSeats?: number;
}

export interface SearchSchedulesRequest {
  fromCityId: string;
  toCityId: string;
  travelDate: string;
}

export interface CreateScheduleRequest {
  routeId: string;
  busId: string;
  travelDate?: string;
  departureTime: string;
  arrivalTime: string;
  pricePerSeat: number;
}

export interface UpdateScheduleRequest {
  departureTime?: string;
  arrivalTime?: string;
}
