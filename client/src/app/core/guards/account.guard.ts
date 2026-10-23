import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AccountService } from '../services/account.service';

/**
 * Route guard for the signed-in customer area (profile, orders, my reviews).
 * Redirects anonymous visitors to the customer login, preserving the intended
 * destination as a returnUrl.
 */
export const accountGuard: CanActivateFn = (_route, state) => {
  const account = inject(AccountService);
  const router = inject(Router);

  if (account.isLoggedIn()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
