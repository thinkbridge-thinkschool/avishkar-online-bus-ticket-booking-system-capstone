export interface FeedbackEntry {
  feedbackId: string;
  bookingId: string;
  userId: string;
  scheduleId: string;
  rating: number;
  comment?: string;
  createdAt: string;
}

export interface CreateFeedbackRequest {
  bookingId: string;
  scheduleId: string;
  rating: number;
  comment?: string;
}

export interface UpdateFeedbackRequest {
  rating?: number;
  comment?: string;
}

export interface FeedbackStats {
  scheduleId: string;
  averageRating: number;
  totalCount: number;
}
