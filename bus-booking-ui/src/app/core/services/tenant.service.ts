import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { Tenant, TenantMetrics, RegisterTenantRequest } from '../../shared/models/tenant.model';

@Injectable({ providedIn: 'root' })
export class TenantService {
  constructor(private readonly http: HttpClient) {}

  async getMyTenant(): Promise<Tenant | null> {
    try {
      return await firstValueFrom(this.http.get<Tenant>('/api/v1/tenants/my'));
    } catch (err: unknown) {
      if (err instanceof HttpErrorResponse && err.status === 404) return null;
      throw err;
    }
  }

  async registerTenant(req: RegisterTenantRequest): Promise<{ tenantId: string }> {
    return firstValueFrom(
      this.http.post<{ tenantId: string }>('/api/v1/tenants/register', req),
    );
  }

  async getAllTenants(): Promise<Tenant[]> {
    return firstValueFrom(this.http.get<Tenant[]>('/api/v1/tenants/'));
  }

  async getPendingTenants(): Promise<Tenant[]> {
    return firstValueFrom(this.http.get<Tenant[]>('/api/v1/tenants/pending'));
  }

  async getTenantMetrics(): Promise<TenantMetrics[]> {
    return firstValueFrom(this.http.get<TenantMetrics[]>('/api/v1/admin/tenants/metrics'));
  }

  async approveTenant(tenantId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/v1/tenants/${tenantId}/approve`, {}),
    );
  }

  async rejectTenant(tenantId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/v1/tenants/${tenantId}/reject`, {}),
    );
  }

  async suspendTenant(tenantId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/v1/tenants/${tenantId}/suspend`, {}),
    );
  }

  async reactivateTenant(tenantId: string): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(`/api/v1/tenants/${tenantId}/reactivate`, {}),
    );
  }
}
