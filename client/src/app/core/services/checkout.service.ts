import { Injectable } from '@angular/core';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse, OrderConfirmation, PlaceOrderRequest } from '../models/api.models';
import { AccountService } from './account.service';

/**
 * Guest checkout data access. Returns the full envelope so the confirmation
 * page can surface the server message (e.g. "Discount code not applied: ...").
 */
@Injectable({ providedIn: 'root' })
export class CheckoutService {
  constructor(private api: ApiService, private account: AccountService) {}

  private storageKey(): string {
    let owner = 'guest';
    const token = this.account.getToken();
    if (token) {
      try { owner = 'customer:' + JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/'))).sub; }
      catch { owner = 'signed-in'; }
    }
    return 'shop_checkout_pending:' + window.location.hostname + ':' + owner;
  }

  pendingRequest(): PlaceOrderRequest | null {
    try { return JSON.parse(localStorage.getItem(this.storageKey()) || 'null'); }
    catch { return null; }
  }

  /** Places a guest order; the server computes trusted totals and a number. */
  placeOrder(request: PlaceOrderRequest): Observable<ApiResponse<OrderConfirmation>> {
    const storageKey = this.storageKey();
    const pending = this.pendingRequest();
    const fingerprint = (value: PlaceOrderRequest) => JSON.stringify({...value, idempotencyKey: undefined});
    if (pending && fingerprint(pending) !== fingerprint(request))
      return throwError(() => ({error:{message:'A previous order submission still needs confirmation. Retry that submission before starting another.'}}));
    const submission = pending || {...request, idempotencyKey: crypto.randomUUID()};
    try { localStorage.setItem(storageKey, JSON.stringify(submission)); }
    catch { return throwError(() => ({error:{message:'Your browser could not save checkout recovery details. Enable site storage and try again; no order was submitted.'}})); }
    return this.api.postEnvelope<OrderConfirmation>('checkout', submission).pipe(
      tap(res => { if (res.success && res.data?.orderNumber) {
        if (res.data.trackingToken) {
          try { localStorage.setItem('shop_tracking_access:'+window.location.hostname+':'+res.data.orderNumber,res.data.trackingToken); } catch { }
        }
        localStorage.removeItem(storageKey);
      } }),
      catchError(err => {
        if (err.status === 400 || err.headers?.get('X-Checkout-Outcome') === 'NotCreated')
          localStorage.removeItem(storageKey);
        return throwError(() => err);
      })
    );
  }
}
