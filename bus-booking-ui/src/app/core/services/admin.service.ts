import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Vendor, VendorStatus } from '../../shared/models/vendor.model';

export interface RecentVendor {
  vendorId: string;
  vendorName: string;
  email: string;
  status: VendorStatus;
  createdAt: string;
}

export interface AdminDashboard {
  totalUsers: number;
  totalVendors: number;
  totalBookings: number;
  totalRevenue: number;
  pendingVendors: number;
  totalTenants: number;
  pendingTenants: number;
  activeTenants: number;
  suspendedTenants: number;
  recentVendors: RecentVendor[];
}

export interface AdminCreateVendorRequest {
  userEmail: string;
  vendorName: string;
  phoneNumber: string;
  address: string;
  licenseNumber: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private readonly http: HttpClient) {}

  async getDashboard(): Promise<AdminDashboard> {
    return firstValueFrom(this.http.get<AdminDashboard>('/api/v1/admin/dashboard'));
  }

  async getPendingVendors(): Promise<Vendor[]> {
    return firstValueFrom(this.http.get<Vendor[]>('/api/v1/vendors/pending'));
  }

  async getAllVendors(): Promise<Vendor[]> {
    return firstValueFrom(this.http.get<Vendor[]>('/api/v1/vendors'));
  }

  async addVendor(cmd: AdminCreateVendorRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/vendors/admin-create', cmd));
  }

  async approveVendor(vendorId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/vendors/${vendorId}/approve`, {}));
  }

  async rejectVendor(vendorId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/vendors/${vendorId}/reject`, {}));
  }

  async deactivateVendor(vendorId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/v1/vendors/${vendorId}/deactivate`, {}));
  }
}
