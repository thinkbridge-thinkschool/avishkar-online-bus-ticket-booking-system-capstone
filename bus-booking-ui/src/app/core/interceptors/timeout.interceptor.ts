import { HttpInterceptorFn } from '@angular/common/http';
import { timeout } from 'rxjs';
import { HTTP_TIMEOUT_MS } from '../tokens/http-timeout.token';

export const timeoutInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(timeout(req.context.get(HTTP_TIMEOUT_MS)));
