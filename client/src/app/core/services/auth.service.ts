import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { AuthenticateRequest, AuthenticateResponse } from '../models/auth.models';

/**
 * Admin authentication. Stores the issued JWT in localStorage and exposes a
 * reactive "is logged in" flag for the admin shell. The token is attached to
 * outgoing requests by the auth interceptor.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private static readonly TOKEN_KEY = 'shop_admin_token';
  private static readonly USER_KEY = 'shop_admin_user';

  private readonly loggedInSubject = new BehaviorSubject<boolean>(this.hasToken());
  readonly loggedIn$: Observable<boolean> = this.loggedInSubject.asObservable();

  constructor(private api: ApiService) {}

  /** Authenticates an admin and persists the JWT on success. */
  login(request: AuthenticateRequest): Observable<AuthenticateResponse> {
    return this.api.post<AuthenticateResponse>('auth/login', request).pipe(
      tap(res => {
        localStorage.setItem(AuthService.TOKEN_KEY, res.token);
        localStorage.setItem(AuthService.USER_KEY, JSON.stringify({ username: res.username, role: res.role }));
        this.loggedInSubject.next(true);
      })
    );
  }

  /** Clears the stored token/user and flips the logged-in flag. */
  logout(): void {
    localStorage.removeItem(AuthService.TOKEN_KEY);
    localStorage.removeItem(AuthService.USER_KEY);
    this.loggedInSubject.next(false);
  }

  /** Returns the raw JWT (or null) for the interceptor. */
  getToken(): string | null {
    return localStorage.getItem(AuthService.TOKEN_KEY);
  }

  /** The signed-in admin's display name, if any. */
  getUsername(): string | null {
    const raw = localStorage.getItem(AuthService.USER_KEY);
    return raw ? (JSON.parse(raw).username as string) : null;
  }

  /** Whether an admin token is currently stored. */
  isLoggedIn(): boolean {
    return this.hasToken();
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(AuthService.TOKEN_KEY);
  }
}
