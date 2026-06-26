export interface UserProfile {
  userId: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  address?: string;
}

export interface UpdateUserProfileRequest {
  fullName?: string;
  phoneNumber?: string;
  address?: string;
}
