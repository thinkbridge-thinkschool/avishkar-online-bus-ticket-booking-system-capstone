export interface BusRoute {
  routeId: string;
  source: string;
  destination: string;
}

export interface CreateRouteRequest {
  source: string;
  destination: string;
}
