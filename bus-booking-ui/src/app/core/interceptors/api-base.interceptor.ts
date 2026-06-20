import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_BASE_URL } from '../tokens/api-base-url.token';

export const apiBaseInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) return next(req);

  const base = inject(API_BASE_URL);
  return next(req.clone({ url: `${base}${req.url}` }));
};
