import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppError } from '../models/app-error';
import type { Vendor, RegisterVendorRequest, RegisterNewVendorRequest, UpdateVendorProfileRequest } from '../../shared/models/vendor.model';
import type { Bus, CreateBusRequest, UpdateBusRequest } from '../../shared/models/bus.model';

@Injectable({ providedIn: 'root' })
export class VendorService {
  constructor(private readonly http: HttpClient) {}

  async register(cmd: RegisterVendorRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/vendors/register', cmd));
  }

  // Public, pre-login signup — creates the account and the vendor profile together.
  async registerNew(cmd: RegisterNewVendorRequest): Promise<{ message: string; vendorId: string }> {
    return firstValueFrom(
      this.http.post<{ message: string; vendorId: string }>('/api/v1/vendors/register-new', cmd),
    );
  }

  async getMyProfile(): Promise<Vendor | null> {
    try {
      return await firstValueFrom(this.http.get<Vendor>('/api/v1/vendors/me'));
    } catch (err: unknown) {
      if (err instanceof AppError && err.status === 404) return null;
      throw err;
    }
  }

  async updateProfile(vendorId: string, cmd: UpdateVendorProfileRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/v1/vendors/${vendorId}`, cmd));
  }

  async getMyBuses(): Promise<Bus[]> {
    try {
      return await firstValueFrom(this.http.get<Bus[]>('/api/v1/buses/mine'));
    } catch (err: unknown) {
      if (err instanceof AppError && err.status === 404) return [];
      throw err;
    }
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
