import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { City, CreateCityRequest } from '../../shared/models/city.model';

@Injectable({ providedIn: 'root' })
export class CityService {
  constructor(private readonly http: HttpClient) {}

  async getCities(): Promise<City[]> {
    return firstValueFrom(this.http.get<City[]>('/api/v1/cities'));
  }

  async createCity(cmd: CreateCityRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/cities', cmd));
  }

  async deleteCity(cityId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/cities/${cityId}`));
  }
}
