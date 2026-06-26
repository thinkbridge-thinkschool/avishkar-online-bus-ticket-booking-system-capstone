import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AppError } from '../models/app-error';
import type { Schedule, SearchSchedulesRequest, CreateScheduleRequest, UpdateScheduleRequest } from '../../shared/models/schedule.model';

@Injectable({ providedIn: 'root' })
export class ScheduleService {
  constructor(private readonly http: HttpClient) {}

  async searchSchedules(params: SearchSchedulesRequest): Promise<Schedule[]> {
    return firstValueFrom(
      this.http.get<Schedule[]>('/api/v1/schedules/search', {
        params: params as unknown as Record<string, string>,
      }) 
    );
  }

  async getById(scheduleId: string): Promise<Schedule> {
    return firstValueFrom(this.http.get<Schedule>(`/api/v1/schedules/${scheduleId}`));
  }

  async getAllSchedules(): Promise<Schedule[]> {
    return firstValueFrom(this.http.get<Schedule[]>('/api/v1/schedules'));
  }

  async getVendorSchedules(): Promise<Schedule[]> {
    try {
      return await firstValueFrom(this.http.get<Schedule[]>('/api/v1/schedules/mine'));
    } catch (err: unknown) {
      if (err instanceof AppError && err.status === 404) return [];
      throw err;
    }
  }

  async createSchedule(cmd: CreateScheduleRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/schedules', cmd));
  }

  async updateSchedule(id: string, cmd: UpdateScheduleRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/v1/schedules/${id}`, cmd));
  }

  async deleteSchedule(id: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/schedules/${id}`));
  }
}
