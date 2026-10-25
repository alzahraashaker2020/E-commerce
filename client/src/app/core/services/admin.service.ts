import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';

/**
 * Admin data access for the JWT-protected api/admin/* endpoints. The auth
 * interceptor attaches the Bearer token automatically. Response shapes are
 * kept loose (any) here because the admin views render generic tables; the
 * customer-facing services use the strict models.
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  constructor(private api: ApiService) {}

  // --- Sales / dashboard ---
  getSalesOverview(): Observable<any> {
    return this.api.get<any>('admin/sales/overview');
  }

  // --- Products ---
  getProducts(): Observable<any[]> {
    return this.api.get<any[]>('admin/products');
  }
  createProduct(body: unknown): Observable<any> {
    return this.api.post<any>('admin/products', body);
  }
  updateProduct(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`admin/products/${id}`, body);
  }
  deleteProduct(id: number): Observable<any> {
    return this.api.delete<any>(`admin/products/${id}`);
  }
  bulkUpdateProducts(body: unknown): Observable<any> {
    return this.api.put<any>('admin/products/bulk', body);
  }

  // --- Categories ---
  getAdminCategories(): Observable<any[]> {
    return this.api.get<any[]>('admin/categories');
  }
  createCategory(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('admin/categories', body);
  }
  updateCategory(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`admin/categories/${id}`, body);
  }
  deleteCategory(id: number): Observable<any> {
    return this.api.delete<any>(`admin/categories/${id}`);
  }

  // --- Sizes ---
  getSizes(): Observable<any[]> {
    return this.api.get<any[]>('admin/sizes');
  }
  createSize(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('admin/sizes', body);
  }
  updateSize(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`admin/sizes/${id}`, body);
  }
  deleteSize(id: number): Observable<any> {
    return this.api.delete<any>(`admin/sizes/${id}`);
  }

  // --- Colors ---
  getColors(): Observable<any[]> {
    return this.api.get<any[]>('admin/colors');
  }
  createColor(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('admin/colors', body);
  }
  updateColor(id: number, body: unknown): Observable<any> {
    return this.api.put<any>(`admin/colors/${id}`, body);
  }
  deleteColor(id: number): Observable<any> {
    return this.api.delete<any>(`admin/colors/${id}`);
  }

  // --- Uploads (multipart/form-data; returns the full envelope so the
  //     validation/400 message surfaces to the caller). ---
  uploadImage(file: File): Observable<any> {
    const data = new FormData();
    data.append('file', file);
    return this.api.postEnvelope<any>('admin/uploads', data);
  }

  // --- Inventory ---
  getLowStock(): Observable<any[]> {
    return this.api.get<any[]>('admin/inventory/low-stock');
  }

  // --- Orders (returns a paged result: { items, totalCount, page, pageSize }) ---
  getOrders(page = 1, pageSize = 50): Observable<any> {
    return this.api.get<any>('admin/orders', { page, pageSize });
  }
  updateOrderStatus(id: number, status: string): Observable<any> {
    return this.api.put<any>(`admin/orders/${id}/status`, { status });
  }

  // --- Discounts ---
  getDiscounts(): Observable<any[]> {
    return this.api.get<any[]>('admin/discounts');
  }
  createDiscount(body: unknown): Observable<any> {
    return this.api.postEnvelope<any>('admin/discounts', body);
  }
  deleteDiscount(id: number): Observable<any> {
    return this.api.delete<any>(`admin/discounts/${id}`);
  }

  // --- Customers ---
  getCustomers(): Observable<any[]> {
    return this.api.get<any[]>('admin/customers');
  }
}
