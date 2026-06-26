export type VendorStatus = 'Pending' | 'Approved' | 'Rejected' | 'Inactive';

export interface Vendor {
  vendorId: string;
  vendorName: string;
  email: string;
  phoneNumber: string;
  address?: string;
  licenseNumber?: string;
  status: VendorStatus;
  isActive: boolean;
}

export interface RegisterVendorRequest {
  vendorName: string;
  email: string;
  phoneNumber: string;
  address: string;
  licenseNumber: string;
}

export interface UpdateVendorProfileRequest {
  vendorName?: string;
  phoneNumber?: string;
  address?: string;
}
