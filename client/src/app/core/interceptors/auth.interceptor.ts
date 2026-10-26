import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MerchantAuthService } from '../services/merchant-auth.service';
import { AccountService } from '../services/account.service';
import { environment } from '../../../environments/environment';

/**
 * Functional HTTP interceptor that attaches the right JWT as a Bearer token:
 * - the MERCHANT token for `api/merchant/*` requests, and
 * - the CUSTOMER token for the protected customer endpoints (account profile/
 *   orders/reviews and creating a review).
 *
 * Public endpoints — catalog, cart, checkout, tracking, the public product
 * review GET, and the account register/login/social calls — are left untouched
 * so they work for anonymous visitors.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(MerchantAuthService);
  const account = inject(AccountService);

  const url = req.url;
  if (!url.startsWith(environment.apiBaseUrl + '/')) return next(req);
  if (url.includes('/platform/')) {
    const token = localStorage.getItem('shop_platform_token');
    return next(token && !url.endsWith('/auth/login') ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req);
  }

  // Merchant endpoints use the merchant token.
  if (url.includes('/merchant/')) {
    const token = auth.getToken();
    if (token && !url.endsWith('/auth/login')) {
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
  const isAccountSecurity = url.endsWith('/account/email-status') || url.endsWith('/account/resend-verification');
  const isCreateReview = url.endsWith('/reviews') && req.method === 'POST';

  if (isProtectedAccount || isAccountSecurity || isCreateReview || url.includes('/ordertracking/') || url.includes('/returns/') || url === environment.apiBaseUrl + '/checkout') {
    const token = account.getToken();
    if (token) {
      return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
    }
  }

  return next(req);
};
