import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import type { FeedbackEntry, CreateFeedbackRequest, UpdateFeedbackRequest, FeedbackStats } from '../../shared/models/feedback.model';

@Injectable({ providedIn: 'root' })
export class FeedbackService {
  constructor(private readonly http: HttpClient) {}

  async createFeedback(cmd: CreateFeedbackRequest): Promise<string> {
    return firstValueFrom(this.http.post<string>('/api/v1/feedback', cmd));
  }

  async updateFeedback(feedbackId: string, cmd: UpdateFeedbackRequest): Promise<void> {
    return firstValueFrom(this.http.put<void>(`/api/v1/feedback/${feedbackId}`, cmd));
  }

  async deleteFeedback(feedbackId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/v1/feedback/${feedbackId}`));
  }

  async getMyFeedback(): Promise<FeedbackEntry[]> {
    return firstValueFrom(this.http.get<FeedbackEntry[]>('/api/v1/feedback/my'));
  }

  async getScheduleStats(scheduleId: string): Promise<FeedbackStats> {
    return firstValueFrom(this.http.get<FeedbackStats>(`/api/v1/feedback/schedule/${scheduleId}/stats`));
  }
}
