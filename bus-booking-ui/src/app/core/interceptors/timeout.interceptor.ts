import { HttpInterceptorFn } from '@angular/common/http';
import { timeout } from 'rxjs';

export const timeoutInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(timeout(30_000)); // 30 seconds timeout for all API requests. If the request takes longer than 30 seconds, it will throw a timeout error.
