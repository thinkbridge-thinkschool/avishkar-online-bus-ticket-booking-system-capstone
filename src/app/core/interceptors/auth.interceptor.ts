import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, switchMap, catchError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/') && !req.url.includes('/api/')) return next(req);

  const auth = inject(AuthService);

  return from(auth.getAccessToken()).pipe(
    switchMap(token => {
      const cloned = token
        ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) })
        : req;
      return next(cloned);
    }),
    catchError(() => next(req))
  );
};
