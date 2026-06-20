import { HttpInterceptorFn } from '@angular/common/http';
import { timeout } from 'rxjs';

export const timeoutInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(timeout(30_000));
