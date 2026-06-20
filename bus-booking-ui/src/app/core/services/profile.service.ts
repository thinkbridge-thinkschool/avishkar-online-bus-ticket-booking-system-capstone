import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { UserProfile, UpdateUserProfileRequest } from '../../shared/models/user.model';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  constructor(private readonly http: HttpClient) {}

  async getProfile(): Promise<UserProfile> {
    return firstValueFrom(this.http.get<UserProfile>('/api/v1/users/profile'));
  }

  async updateProfile(cmd: UpdateUserProfileRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>('/api/v1/users/profile', cmd));
  }
}
