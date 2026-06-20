import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Vendor } from '../../shared/models/vendor.model';

export interface AdminDashboard {
  totalUsers: number;
  totalVendors: number;
  totalBookings: number;
  totalRevenue: number;
  pendingVendors: number;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private readonly http: HttpClient) {}

  async getDashboard(): Promise<AdminDashboard> {
    return firstValueFrom(this.http.get<AdminDashboard>('/api/v1/admin/dashboard'));
  }

  async getPendingVendors(): Promise<Vendor[]> {
    return firstValueFrom(this.http.get<Vendor[]>('/api/v1/vendors?status=Pending'));
  }

  async getAllVendors(): Promise<Vendor[]> {
    return firstValueFrom(this.http.get<Vendor[]>('/api/v1/vendors'));
  }

  async approveVendor(vendorId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/vendors/${vendorId}/approve`, {}));
  }

  async rejectVendor(vendorId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/vendors/${vendorId}/reject`, {}));
  }
}
