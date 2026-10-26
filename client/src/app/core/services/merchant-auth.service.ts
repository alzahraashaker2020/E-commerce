import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthenticateRequest, AuthenticateResponse } from '../models/auth.models';

/**
 * Merchant authentication. Stores the issued JWT in localStorage and exposes a
 * reactive "is logged in" flag for the merchant shell. The token is attached to
 * outgoing requests by the auth interceptor.
 */
@Injectable({ providedIn: 'root' })
export class MerchantAuthService {
  private static readonly TOKEN_KEY = 'shop_merchant_token:' + window.location.hostname;
  private static readonly USER_KEY = 'shop_merchant_user:' + window.location.hostname;

  private readonly loggedInSubject = new BehaviorSubject<boolean>(this.hasToken());
  readonly loggedIn$: Observable<boolean> = this.loggedInSubject.asObservable();

  constructor(private api: ApiService) {}

  /** Authenticates an merchant and persists the JWT on success. */
  login(request: AuthenticateRequest): Observable<AuthenticateResponse> {
    return this.api.post<AuthenticateResponse>('merchant/auth/login', request).pipe(
      tap(res => {
        localStorage.setItem(MerchantAuthService.TOKEN_KEY, res.token);
        localStorage.setItem(MerchantAuthService.USER_KEY, JSON.stringify({ username: res.username, role: res.role }));
        this.loggedInSubject.next(true);
      })
    );
  }

  /** Clears the stored token/user and flips the logged-in flag. */
  logout(): void {
    localStorage.removeItem(MerchantAuthService.TOKEN_KEY);
    localStorage.removeItem(MerchantAuthService.USER_KEY);
    this.loggedInSubject.next(false);
  }

  /** Returns the raw JWT (or null) for the interceptor. */
  getToken(): string | null {
    return localStorage.getItem(MerchantAuthService.TOKEN_KEY);
  }

  /** The signed-in merchant's display name, if any. */
  getUsername(): string | null {
    const raw = localStorage.getItem(MerchantAuthService.USER_KEY);
    return raw ? (JSON.parse(raw).username as string) : null;
  }

  /** Whether an merchant token is currently stored. */
  isLoggedIn(): boolean {
    return this.hasToken();
  }

  private hasToken(): boolean {
    const token = localStorage.getItem(MerchantAuthService.TOKEN_KEY);
    try {
      const user = JSON.parse(localStorage.getItem(MerchantAuthService.USER_KEY) || '{}');
      const payload = JSON.parse(atob((token || '').split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
      return user.role === 'Merchant' && payload.exp * 1000 > Date.now();
    } catch { return false; }
  }
}
