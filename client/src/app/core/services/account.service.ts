import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import {
  CreateReviewRequest,
  CustomerAuthResponse,
  CustomerLoginRequest,
  CustomerProfile,
  CustomerRegisterRequest,
  ProductReviewSummary,
  Review,
  SocialLoginRequest
} from '../models/account.models';
import { OrderTracking } from '../models/api.models';

/**
 * Customer account access and session state. Stores the customer JWT in
 * localStorage (separate key from the admin token) and exposes a reactive
 * logged-in flag and display name for the header. The customer token is
 * attached to protected requests by the customer auth interceptor.
 */
@Injectable({ providedIn: 'root' })
export class AccountService {
  private static readonly TOKEN_KEY = 'shop_customer_token';
  private static readonly USER_KEY = 'shop_customer_user';

  private readonly loggedInSubject = new BehaviorSubject<boolean>(this.hasToken());
  private readonly nameSubject = new BehaviorSubject<string | null>(this.storedName());

  /** Reactive "customer is signed in" flag. */
  readonly loggedIn$: Observable<boolean> = this.loggedInSubject.asObservable();
  /** Reactive customer display name. */
  readonly name$: Observable<string | null> = this.nameSubject.asObservable();

  constructor(private api: ApiService) {}

  // --- Auth ---

  register(request: CustomerRegisterRequest): Observable<CustomerAuthResponse> {
    return this.api.post<CustomerAuthResponse>('account/register', request).pipe(
      tap(res => this.persistSession(res))
    );
  }

  login(request: CustomerLoginRequest): Observable<CustomerAuthResponse> {
    return this.api.post<CustomerAuthResponse>('account/login', request).pipe(
      tap(res => this.persistSession(res))
    );
  }

  socialLogin(request: SocialLoginRequest): Observable<CustomerAuthResponse> {
    return this.api.post<CustomerAuthResponse>('account/social', request).pipe(
      tap(res => this.persistSession(res))
    );
  }

  logout(): void {
    localStorage.removeItem(AccountService.TOKEN_KEY);
    localStorage.removeItem(AccountService.USER_KEY);
    this.loggedInSubject.next(false);
    this.nameSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(AccountService.TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return this.hasToken();
  }

  // --- Profile / orders / reviews (protected) ---

  getProfile(): Observable<CustomerProfile> {
    return this.api.get<CustomerProfile>('account/profile');
  }

  updateProfile(profile: CustomerProfile): Observable<CustomerProfile> {
    return this.api.put<CustomerProfile>('account/profile', profile);
  }

  getOrders(): Observable<OrderTracking[]> {
    return this.api.get<OrderTracking[]>('account/orders');
  }

  getMyReviews(): Observable<Review[]> {
    return this.api.get<Review[]>('account/reviews');
  }

  // --- Reviews ---

  /** Public: product review summary (average + list). */
  getProductReviews(productId: number): Observable<ProductReviewSummary> {
    return this.api.get<ProductReviewSummary>(`reviews/product/${productId}`);
  }

  /** Customer-only: create a product review (server gates to purchasers). */
  createReview(request: CreateReviewRequest): Observable<Review> {
    return this.api.post<Review>('reviews', request);
  }

  // --- internal ---

  private persistSession(res: CustomerAuthResponse): void {
    localStorage.setItem(AccountService.TOKEN_KEY, res.token);
    localStorage.setItem(AccountService.USER_KEY, JSON.stringify({ name: res.fullName, email: res.email }));
    this.loggedInSubject.next(true);
    this.nameSubject.next(res.fullName);
  }

  private hasToken(): boolean {
    return !!localStorage.getItem(AccountService.TOKEN_KEY);
  }

  private storedName(): string | null {
    const raw = localStorage.getItem(AccountService.USER_KEY);
    return raw ? (JSON.parse(raw).name as string) : null;
  }
}
