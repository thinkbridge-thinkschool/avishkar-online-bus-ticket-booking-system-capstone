import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { BusRoute, CreateRouteRequest } from '../../shared/models/route.model';

@Injectable({ providedIn: 'root' })
export class RouteService {
  constructor(private readonly http: HttpClient) {}

  async getRoutes(): Promise<BusRoute[]> {
    return firstValueFrom(this.http.get<BusRoute[]>('/api/v1/routes'));
  }

  async createRoute(cmd: CreateRouteRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/routes', cmd));
  }

  async deleteRoute(routeId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/routes/${routeId}`));
  }
}
