import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { AccountService } from '../services/account.service';

/**
 * Functional HTTP interceptor that attaches the right JWT as a Bearer token:
 * - the ADMIN token for `api/admin/*` requests, and
 * - the CUSTOMER token for the protected customer endpoints (account profile/
 *   orders/reviews and creating a review).
 *
 * Public endpoints — catalog, cart, checkout, tracking, the public product
 * review GET, and the account register/login/social calls — are left untouched
 * so they work for anonymous visitors.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const account = inject(AccountService);

  const url = req.url;

  // Admin endpoints use the admin token.
  if (url.includes('/admin/')) {
    const token = auth.getToken();
    if (token) {
      return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
    }
    return next(req);
  }

  // Protected customer endpoints use the customer token. These are the account
  // profile/orders/reviews reads/writes and creating a review (POST api/reviews).
  // The public endpoints (register/login/social and GET reviews/product/{id})
  // are deliberately excluded so anonymous visitors are unaffected.
  const isProtectedAccount =
    url.includes('/account/profile') ||
    url.includes('/account/orders') ||
    url.includes('/account/reviews');
  const isCreateReview = url.endsWith('/reviews') && req.method === 'POST';

  if (isProtectedAccount || isCreateReview) {
    const token = account.getToken();
    if (token) {
      return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
    }
  }

  return next(req);
};
