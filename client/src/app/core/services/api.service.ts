import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api.models';

/**
 * Thin HTTP wrapper around the backend. Every endpoint returns an
 * ApiResponse<T> envelope; this service unwraps the `data` field so callers
 * work with the payload directly, while still centralising the base URL.
 */
@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private http: HttpClient) {}

  /** Builds an absolute API URL from a relative path (e.g. 'catalog'). */
  private url(path: string): string {
    return `${environment.apiBaseUrl}/${path}`;
  }

  /** GET that unwraps the ApiResponse envelope and yields the payload. */
  get<T>(path: string, params?: Record<string, string | number | boolean | undefined | null>): Observable<T> {
    return this.http
      .get<ApiResponse<T>>(this.url(path), { params: this.toParams(params) })
      .pipe(map(r => r.data));
  }

  /** GET that returns the full envelope (for callers that need message/status). */
  getEnvelope<T>(path: string, params?: Record<string, string | number | boolean | undefined | null>): Observable<ApiResponse<T>> {
    return this.http.get<ApiResponse<T>>(this.url(path), { params: this.toParams(params) });
  }

  /** POST that unwraps the ApiResponse envelope and yields the payload. */
  post<T>(path: string, body: unknown): Observable<T> {
    return this.http.post<ApiResponse<T>>(this.url(path), body).pipe(map(r => r.data));
  }

  /** POST that returns the full envelope. */
  postEnvelope<T>(path: string, body: unknown): Observable<ApiResponse<T>> {
    return this.http.post<ApiResponse<T>>(this.url(path), body);
  }

  /** PUT that unwraps the ApiResponse envelope. */
  put<T>(path: string, body: unknown): Observable<T> {
    return this.http.put<ApiResponse<T>>(this.url(path), body).pipe(map(r => r.data));
  }

  /** DELETE that unwraps the ApiResponse envelope. */
  delete<T>(path: string): Observable<T> {
    return this.http.delete<ApiResponse<T>>(this.url(path)).pipe(map(r => r.data));
  }

  /** Converts a plain object into HttpParams, skipping null/undefined values. */
  private toParams(params?: Record<string, string | number | boolean | undefined | null>): HttpParams {
    let httpParams = new HttpParams();
    if (!params) {
      return httpParams;
    }
    for (const [key, value] of Object.entries(params)) {
      if (value !== undefined && value !== null) {
        httpParams = httpParams.set(key, String(value));
      }
    }
    return httpParams;
  }
}
