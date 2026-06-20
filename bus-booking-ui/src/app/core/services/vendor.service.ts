import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Vendor, RegisterVendorRequest, UpdateVendorProfileRequest } from '../../shared/models/vendor.model';
import type { Bus, CreateBusRequest, UpdateBusRequest } from '../../shared/models/bus.model';

@Injectable({ providedIn: 'root' })
export class VendorService {
  constructor(private readonly http: HttpClient) {}

  async register(cmd: RegisterVendorRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/vendors/register', cmd));
  }

  async getMyProfile(): Promise<Vendor> {
    return firstValueFrom(this.http.get<Vendor>('/api/v1/vendors/profile'));
  }

  async updateProfile(cmd: UpdateVendorProfileRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>('/api/v1/vendors/profile', cmd));
  }

  async getMyBuses(): Promise<Bus[]> {
    return firstValueFrom(this.http.get<Bus[]>('/api/v1/buses'));
  }

  async addBus(cmd: CreateBusRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/buses', cmd));
  }

  async updateBus(busId: string, cmd: UpdateBusRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/v1/buses/${busId}`, cmd));
  }

  async deleteBus(busId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/buses/${busId}`));
  }
}
