import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { throwError, timer } from 'rxjs';
import { retry } from 'rxjs/operators';
import { RETRY_CONFIG } from '../tokens/retry-config.token';

const TRANSIENT = new Set([0, 429, 500, 502, 503, 504]);

export const retryInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.method !== 'GET') return next(req);

  const { maxRetries, baseDelayMs } = inject(RETRY_CONFIG);

  return next(req).pipe(
    retry({
      count: maxRetries,
      delay: (err, attempt) => {
        const status = (err as { status?: number })?.status ?? 0;
        return TRANSIENT.has(status)
          ? timer(baseDelayMs * 2 ** (attempt - 1))
          : throwError(() => err);
      },
    })
  );
};
