import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { Category, PagedResult, ProductDetail, ProductListItem } from '../models/api.models';

/**
 * Storefront catalog data access: paged product listing (with optional category
 * filter), single-product detail, and the category list. Maps 1:1 onto the
 * backend CatalogController and CategoriesController.
 */
@Injectable({ providedIn: 'root' })
export class CatalogService {
  constructor(private api: ApiService) {}

  /**
   * Fetches a page of products. The server returns the total count so the
   * catalog component can preserve browse position across navigation.
   */
  getProducts(page: number, pageSize: number, categoryId?: number | null): Observable<PagedResult<ProductListItem>> {
    return this.api.get<PagedResult<ProductListItem>>('catalog', { page, pageSize, categoryId });
  }

  /** Fetches full detail for a single product. */
  getProduct(id: number): Observable<ProductDetail> {
    return this.api.get<ProductDetail>(`catalog/${id}`);
  }

  /** Fetches all categories (flat) for the filter chips. */
  getCategories(): Observable<Category[]> {
    return this.api.get<Category[]>('categories');
  }

  /** Fetches the top-level categories with their subcategories (home cards). */
  getCategoryTree(): Observable<Category[]> {
    return this.api.get<Category[]>('categories/tree');
  }

  /** Fetches a single top-level category by slug, with its subcategories. */
  getCategoryBySlug(slug: string): Observable<Category> {
    return this.api.get<Category>(`categories/${encodeURIComponent(slug)}`);
  }
}
