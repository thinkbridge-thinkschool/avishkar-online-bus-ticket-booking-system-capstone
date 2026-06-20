export type BusType = 'Sleeper' | 'SemiSleeper' | 'Seater' | 'AC' | 'NonAC';

export interface Bus {
  busId: string;
  vendorId: string;
  busNumber: string;
  busType: BusType;
  totalSeats: number;
  amenities: string[];
}

export interface CreateBusRequest {
  busNumber: string;
  busType: BusType;
  totalSeats: number;
  amenities?: string[];
}

export interface UpdateBusRequest {
  busNumber?: string;
  busType?: BusType;
  totalSeats?: number;
  amenities?: string[];
}
