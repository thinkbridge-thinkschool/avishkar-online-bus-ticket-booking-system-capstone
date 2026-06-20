import { InjectionToken } from '@angular/core';

export interface RetryConfig {
  maxRetries: number;
  baseDelayMs: number;
}

export const RETRY_CONFIG = new InjectionToken<RetryConfig>('RETRY_CONFIG', {
  providedIn: 'root',
  factory: () => ({ maxRetries: 3, baseDelayMs: 500 }),
});
