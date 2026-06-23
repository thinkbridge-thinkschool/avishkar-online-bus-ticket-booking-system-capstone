import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const superAdminGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated() && auth.isSuperAdmin()) return true;
  if (!auth.isAuthenticated())
    return router.createUrlTree(['/'], { queryParams: { returnUrl: state.url } });
  return router.createUrlTree(['/']);
};
