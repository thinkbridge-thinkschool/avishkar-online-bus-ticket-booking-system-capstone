import { HttpContextToken } from '@angular/common/http';

// Per-request override for timeoutInterceptor's default. Most endpoints are plain CRUD calls
// that should fail fast (30s), but a couple (the AI assistant, which can make 1-2 sequential
// upstream LLM calls) need more headroom — set via `context: new HttpContext().set(HTTP_TIMEOUT_MS, 60_000)`
// on that specific request instead of loosening the timeout for every request in the app.
export const HTTP_TIMEOUT_MS = new HttpContextToken<number>(() => 30_000);
