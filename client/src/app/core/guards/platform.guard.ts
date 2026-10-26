import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const platformGuard: CanActivateFn = () => {
  const router = inject(Router);
  try {
    const token = localStorage.getItem('shop_platform_token') || '';
    const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
    if (payload.exp * 1000 > Date.now()) return true;
  } catch { }
  return router.createUrlTree(['/platform']);
};
