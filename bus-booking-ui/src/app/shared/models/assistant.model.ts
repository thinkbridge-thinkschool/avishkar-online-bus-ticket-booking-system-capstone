export interface AssistantHistoryMessage {
  role: 'user' | 'model';
  text: string;
}

export interface AssistantToolResult {
  kind: string;
  dataJson: string;
}

export interface AssistantChatResponse {
  reply: string;
  toolResults: AssistantToolResult[];
}

// UI-side representation of one chat bubble, distinct from AssistantHistoryMessage
// (which is only the role/text pair the backend needs to replay context).
export interface AssistantChatMessage {
  role: 'user' | 'assistant';
  text: string;
  toolResults?: AssistantToolResult[];
  isError?: boolean;
}

export interface AssistantScheduleResult {
  scheduleId: string;
  busName: string;
  busNumber: string;
  source: string;
  destination: string;
  travelDate: string;
  departureTime: string;
  arrivalTime: string;
  availableSeats: number;
  minSeatPrice: number | null;
  busType: string;
}

export interface AssistantBookingResult {
  bookingId: string;
  status: string;
  totalAmount: number;
  fromCityName?: string;
  toCityName?: string;
  travelDate?: string;
  departureTime?: string;
  busName?: string;
  busNumber?: string;
}

export interface AssistantCancelSuggestion {
  bookingId: string;
  status: string;
}

export interface AssistantVendorBus {
  id: string;
  busNumber: string;
  busName: string;
  busType: string;
  totalSeats: number;
}

export interface AssistantVendorSchedule {
  scheduleId: string;
  busName: string;
  busNumber: string;
  source: string;
  destination: string;
  travelDate: string;
}
