import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';
import { StoreSettings } from '../models/api.models';

/**
 * Loads and caches the store-wide currency settings (GET api/settings) so the
 * storefront can format prices in the configured currency. The initial load is
 * triggered once from AppComponent; callers read the cached values via
 * currencyCode()/currencySymbol(), which fall back to USD/$ until loaded.
 */
@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly settings$ = new BehaviorSubject<StoreSettings>({
    currencyCode: 'USD',
    currencySymbol: '$'
  });

  constructor(private api: ApiService) {}

  /**
   * Fetches the store settings once and caches the result. Safe to call from
   * AppComponent on startup; subsequent reads use the cached value.
   */
  load(): Observable<StoreSettings> {
    return this.api.get<StoreSettings>('settings').pipe(
      tap(s => {
        if (s) {
          this.settings$.next(s);
        }
      })
    );
  }

  /** Observable stream of the current store settings. */
  settings(): Observable<StoreSettings> {
    return this.settings$.asObservable();
  }

  /** Current store currency code, defaulting to 'USD' until loaded. */
  currencyCode(): string {
    return this.settings$.value.currencyCode || 'USD';
  }

  /** Current store currency symbol, defaulting to '$' until loaded. */
  currencySymbol(): string {
    return this.settings$.value.currencySymbol || '$';
  }
}
