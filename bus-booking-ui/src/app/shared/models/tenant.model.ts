export type TenantStatus =
  | 'PendingApproval'
  | 'Active'
  | 'Suspended'
  | 'Rejected'
  | 'Deactivated';

export interface Tenant {
  tenantId: string;
  name: string;
  subdomain: string;
  adminEmail: string;
  status: TenantStatus;
  approvedAt: string | null;
  hasRazorpayCredentials: boolean;
  createdAt: string;
}

export interface TenantMetrics {
  tenantId: string;
  name: string;
  subdomain: string;
  status: string;
  bookingCount: number;
  totalRevenue: number;
}

export interface RegisterTenantRequest {
  name: string;
  subdomain: string;
  adminEmail: string;
}
