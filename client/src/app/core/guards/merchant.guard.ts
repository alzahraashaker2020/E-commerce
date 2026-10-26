import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RuntimeService } from '../services/runtime.service';
import { MerchantAuthService } from '../services/merchant-auth.service';

/**
 * Route guard for the merchant area. Redirects unauthenticated visitors to the
 * merchant login page, preserving the intended destination as a returnUrl.
 */
export const merchantGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(MerchantAuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) {
    try{await inject(RuntimeService).refreshStore();return true;}catch{return router.createUrlTree(['/merchant/register']);}
  }

  return router.createUrlTree(['/merchant/login'], { queryParams: { returnUrl: state.url } });
};
