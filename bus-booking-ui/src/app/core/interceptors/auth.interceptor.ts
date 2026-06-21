import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') && !req.url.includes('/api/')) return next(req);

  const auth = inject(AuthService);

  return from(auth.getAccessToken()).pipe(
    switchMap(token => {
      const cloned = token
        ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
        : req;
      return next(cloned).pipe(
        catchError(err => {
          // Always force-refresh and retry on 401:
          // - handles stale cached tokens (v1 audience mismatch)
          // - handles null token race condition (MSAL not yet initialized when request fires)
          if (err.status === 401) {
            return from(auth.getAccessTokenForced()).pipe(
              switchMap(fresh => {
                if (!fresh) return throwError(() => err);
                const retried = req.clone({ headers: req.headers.set('Authorization', `Bearer ${fresh}`) });
                return next(retried);
              }),
              catchError(() => throwError(() => err)),
            );
          }
          return throwError(() => err);
        }),
      );
    }),
    catchError(() => next(req))
  );
};
