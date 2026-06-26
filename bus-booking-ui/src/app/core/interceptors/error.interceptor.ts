import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { toAppError } from '../models/app-error';

export const errorInterceptor: HttpInterceptorFn = (req, next) =>
  next(req).pipe(catchError(err => throwError(() => toAppError(err))));
