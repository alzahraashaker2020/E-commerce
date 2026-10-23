import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogService } from '../../core/services/catalog.service';
import { CartService } from '../../core/services/cart.service';
import { CatalogStateService } from '../../core/services/catalog-state.service';
import { Category, PagedResult, ProductListItem } from '../../core/models/api.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Storefront home/catalog page.
 *
 * Shows, under the hero: the four top-level category cards (from the category
 * tree) and a paginated product grid of everything. Selecting a category chip
 * filters the grid; the "Browse categories" cards link to the dedicated
 * subcategory drill-down page.
 *
 * Preserved-position pagination (the key differentiator): the current page,
 * selected category, and scroll offset are persisted via CatalogStateService.
 * When the shopper opens a product and navigates back, the catalog restores the
 * exact page and scroll position instead of resetting to page 1 / top.
 */
@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.css'
})
export class CatalogComponent implements OnInit {
  /** Top-level categories (for the home cards). */
  topCategories: Category[] = [];
  /** Flat category list (for the filter chips). */
  categories: Category[] = [];
  result: PagedResult<ProductListItem> | null = null;
  loading = false;
  error: string | null = null;

  page = 1;
  pageSize = 8;
  categoryId: number | null = null;

  constructor(
    private catalog: CatalogService,
    private cart: CartService,
    private state: CatalogStateService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    // Top-level categories for the cards.
    this.catalog.getCategoryTree().subscribe({
      next: tree => (this.topCategories = tree),
      error: () => (this.topCategories = [])
    });

    // Flat list for the filter chips.
    this.catalog.getCategories().subscribe({
      next: cats => (this.categories = cats),
      error: () => (this.categories = [])
    });

    // Restore a saved browse position when returning from a product page;
    // otherwise honour any query params, otherwise start fresh at page 1.
    const saved = this.state.get();
    const qpPage = Number(this.route.snapshot.queryParamMap.get('page'));
    const qpCat = this.route.snapshot.queryParamMap.get('categoryId');

    if (saved) {
      this.page = saved.page;
      this.pageSize = saved.pageSize;
      this.categoryId = saved.categoryId;
      this.load(saved.scrollY);
    } else {
      this.page = qpPage && qpPage > 0 ? qpPage : 1;
      this.categoryId = qpCat ? Number(qpCat) : null;
      this.load(0);
    }
  }

  /** Loads the current page, then restores scroll once rendered. */
  load(restoreScrollY = 0): void {
    this.loading = true;
    this.error = null;
    this.catalog.getProducts(this.page, this.pageSize, this.categoryId).subscribe({
      next: res => {
        this.result = res;
        this.loading = false;
        this.syncUrl();
        // Defer until the grid has rendered, then restore the scroll offset.
        if (restoreScrollY > 0) {
          setTimeout(() => window.scrollTo({ top: restoreScrollY, behavior: 'auto' }), 0);
        }
      },
      error: () => {
        this.error = 'Could not load products. Is the API running on http://localhost:5000?';
        this.loading = false;
      }
    });
  }

  /** Smoothly scrolls the hero CTA down to the product grid. */
  scrollToGrid(): void {
    document.getElementById('catalog-grid')?.scrollIntoView({ behavior: 'smooth' });
  }

  /** Changes the category filter and resets to page 1. */
  selectCategory(id: number | null): void {
    this.categoryId = id;
    this.page = 1;
    this.state.clear();
    this.load(0);
    document.getElementById('catalog-grid')?.scrollIntoView({ behavior: 'smooth' });
  }

  /** Navigates to a different page (top of grid). */
  goToPage(p: number): void {
    if (p < 1 || (this.result && p > this.totalPages)) {
      return;
    }
    this.page = p;
    this.load(0);
    document.getElementById('catalog-grid')?.scrollIntoView({ behavior: 'smooth' });
  }

  /** Opens a product, first saving the exact browse position to restore later. */
  openProduct(id: number): void {
    this.state.save({
      page: this.page,
      pageSize: this.pageSize,
      categoryId: this.categoryId,
      scrollY: window.scrollY
    });
    this.router.navigate(['/product', id]);
  }

  /** Adds a product to the cart directly from the grid. */
  quickAdd(product: ProductListItem, event: Event): void {
    event.stopPropagation();
    this.cart.add(product.id, 1, null);
  }

  get totalPages(): number {
    if (!this.result || this.result.pageSize === 0) {
      return 1;
    }
    return Math.max(1, Math.ceil(this.result.totalCount / this.result.pageSize));
  }

  get pageNumbers(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  /** Reflects page/category in the URL so the position is shareable/bookmarkable. */
  private syncUrl(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: this.page, categoryId: this.categoryId ?? null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}
