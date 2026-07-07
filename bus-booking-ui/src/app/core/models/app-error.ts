import { HttpErrorResponse } from '@angular/common/http';
import { TimeoutError } from 'rxjs';

export class AppError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly errors: Record<string, string[]> = {}
  ) {
    super(message);
    this.name = 'AppError';
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function toAppError(err: unknown): AppError {
  if (err instanceof AppError) return err;

  // RxJS's timeout() operator throws a bare TimeoutError with no HTTP context — surface
  // something a user can act on instead of the raw "Timeout has occurred".
  if (err instanceof TimeoutError) {
    return new AppError(0, 'The request took too long to respond. Please try again.');
  }

  if (err instanceof HttpErrorResponse) {
    const body = err.error as ProblemDetails | null;
    const message =
      body?.detail ?? body?.title ?? err.message ?? 'An unexpected error occurred.';
    return new AppError(err.status, message, body?.errors ?? {});
  }

  const message = err instanceof Error ? err.message : 'An unexpected error occurred.';
  return new AppError(0, message);
}
