import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

/**
 * Merchant data access for the JWT-protected api/merchant/* endpoints. The auth
 * interceptor attaches the Bearer token automatically. Response shapes are
 * kept loose (any) here because the merchant views render generic tables; the
 * customer-facing services use the strict models.
 */
@Injectable({ providedIn: 'root' })
export class MerchantService {
  constructor(private api: ApiService) {}

  // --- Sales / dashboard ---
  getSalesOverview(): Observable<any> {
    return this.api.get<any>('merchant/sales/overview');
  }

  // --- Products ---
  getProducts(): Observable<any[]> {
    return this.api.get<any[]>('merchant/products');
  }
  createProduct(body: unknown): Observable<any> {
    return this.api.post<any>('merchant/products', body);
  }
  updateProduct(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`merchant/products/${id}`, body);
  }
  deleteProduct(id: number): Observable<any> {
    return this.api.delete<any>(`merchant/products/${id}`);
  }
  bulkUpdateProducts(body: unknown): Observable<any> {
    return this.api.put<any>('merchant/products/bulk', body);
  }

  // --- Categories ---
  getMerchantCategories(): Observable<any[]> {
    return this.api.get<any[]>('merchant/categories');
  }
  createCategory(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('merchant/categories', body);
  }
  updateCategory(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`merchant/categories/${id}`, body);
  }
  deleteCategory(id: number): Observable<any> {
    return this.api.delete<any>(`merchant/categories/${id}`);
  }

  // --- Sizes ---
  getSizes(): Observable<any[]> {
    return this.api.get<any[]>('merchant/sizes');
  }
  createSize(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('merchant/sizes', body);
  }
  updateSize(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`merchant/sizes/${id}`, body);
  }
  deleteSize(id: number): Observable<any> {
    return this.api.delete<any>(`merchant/sizes/${id}`);
  }

  // --- Colors ---
  getColors(): Observable<any[]> {
    return this.api.get<any[]>('merchant/colors');
  }
  createColor(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('merchant/colors', body);
  }
  updateColor(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`merchant/colors/${id}`, body);
  }
  deleteColor(id: number): Observable<any> {
    return this.api.delete<any>(`merchant/colors/${id}`);
  }

  // --- Uploads (multipart/form-data; returns the full envelope so the
  //     validation/400 message surfaces to the caller). ---
  uploadImage(file: File): Observable<any> {
    const data = new FormData();
    data.append('file', file);
    return this.api.postEnvelope<any>('merchant/uploads', data);
  }

  // --- Inventory ---
  getLowStock(): Observable<any[]> {
    return this.api.get<any[]>('merchant/inventory/low-stock');
  }

  // --- Orders (returns a paged result: { items, totalCount, page, pageSize }) ---
  getOrders(page = 1, pageSize = 50): Observable<any> {
    return this.api.get<any>('merchant/orders', { page, pageSize });
  }
  updateOrderStatus(id: number, status: string): Observable<any> {
    return this.api.put<any>(`merchant/orders/${id}/status`, { status });
  }

  // --- Discounts ---
  getDiscounts(): Observable<any[]> {
    return this.api.get<any[]>('merchant/discounts');
  }
  createDiscount(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('merchant/discounts', body);
  }
  deleteDiscount(id: number): Observable<any> {
    return this.api.delete<any>(`merchant/discounts/${id}`);
  }

  // --- Customers ---
  getCustomers(): Observable<any[]> {
    return this.api.get<any[]>('merchant/customers');
  }
}
