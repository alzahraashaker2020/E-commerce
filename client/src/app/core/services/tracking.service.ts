import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { OrderTracking } from '../models/api.models';

/** First-party order tracking: look up an order by its order number. */
@Injectable({ providedIn: 'root' })
export class TrackingService {
  constructor(private api: ApiService) {}

  /** Fetches the tracking view for an order number. */
  track(orderNumber: string): Observable<OrderTracking> {
    return this.api.get<OrderTracking>(`ordertracking/${encodeURIComponent(orderNumber)}`);
  }

  /** Fetches the tracking view for the most recent order placed with an email. */
  trackByEmail(email: string): Observable<OrderTracking> {
    return this.api.get<OrderTracking>('ordertracking/by-email', { email });
  }
}
