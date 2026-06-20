export interface BusRoute {
  routeId: string;
  fromCityId: string;
  toCityId: string;
  fromCityName?: string;
  toCityName?: string;
  estimatedMinutes: number;
}

export interface CreateRouteRequest {
  fromCityId: string;
  toCityId: string;
  estimatedMinutes: number;
}
