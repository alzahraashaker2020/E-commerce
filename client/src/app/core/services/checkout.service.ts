import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { ApiResponse, OrderConfirmation, PlaceOrderRequest } from '../models/api.models';

/**
 * Guest checkout data access. Returns the full envelope so the confirmation
 * page can surface the server message (e.g. "Discount code not applied: ...").
 */
@Injectable({ providedIn: 'root' })
export class CheckoutService {
  constructor(private api: ApiService) {}

  /** Places a guest order; the server computes trusted totals and a number. */
  placeOrder(request: PlaceOrderRequest): Observable<ApiResponse<OrderConfirmation>> {
    return this.api.postEnvelope<OrderConfirmation>('checkout', request);
  }
}
