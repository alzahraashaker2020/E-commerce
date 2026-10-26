import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { OrderTracking } from '../models/api.models';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.models';

/** First-party order tracking: look up an order by its order number. */
@Injectable({ providedIn: 'root' })
export class TrackingService {
  constructor(private http: HttpClient) {}

  /** Fetches the tracking view for an order number. */
  track(orderNumber: string, accessCode = ''): Observable<OrderTracking> {
    return this.http.get<ApiResponse<OrderTracking>>(`${environment.apiBaseUrl}/ordertracking/${encodeURIComponent(orderNumber)}`,
      {headers: accessCode ? {'X-Order-Tracking-Token':accessCode} : {}}).pipe(map(response=>response.data));
  }

  /** Fetches the tracking view for the most recent order placed with an email. */
}
