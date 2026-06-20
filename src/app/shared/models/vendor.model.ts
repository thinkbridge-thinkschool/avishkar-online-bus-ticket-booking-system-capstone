export type VendorStatus = 'Pending' | 'Approved' | 'Rejected' | 'Inactive';

export interface Vendor {
  vendorId: string;
  email: string;
  companyName: string;
  phoneNumber: string;
  address?: string;
  status: VendorStatus;
  registeredAt: string;
}

export interface RegisterVendorRequest {
  email: string;
  companyName: string;
  phoneNumber: string;
  address?: string;
}

export interface UpdateVendorProfileRequest {
  companyName?: string;
  phoneNumber?: string;
  address?: string;
}
