import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { HTTP_TIMEOUT_MS } from '../tokens/http-timeout.token';
import type { AssistantChatResponse, AssistantHistoryMessage } from '../../shared/models/assistant.model';

// A chat turn can involve 1-2+ sequential upstream LLM calls (tool-call round-trips). Measured
// latency against Gemini's free tier ranged from ~6s to 75s+ under real (self-induced, from
// repeated testing) throttling — well past the app's default 30s request timeout. Override it
// for just this endpoint rather than loosening the default for every request (booking/payment
// calls should still fail fast). No fixed number fully absorbs a provider that's occasionally
// slow for a whole minute-plus; this is deliberately generous headroom, not a guarantee.
const ASSISTANT_TIMEOUT_MS = 90_000;

@Injectable({ providedIn: 'root' })
export class AssistantService {
  private readonly http = inject(HttpClient);

  async chat(message: string, history: AssistantHistoryMessage[]): Promise<AssistantChatResponse> {
    return firstValueFrom(
      this.http.post<AssistantChatResponse>('/api/v1/assistant/chat', { message, history }, {
        context: new HttpContext().set(HTTP_TIMEOUT_MS, ASSISTANT_TIMEOUT_MS),
      }),
    );
  }
}
