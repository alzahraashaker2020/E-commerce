import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CatalogService } from '../../core/services/catalog.service';
import { CartService } from '../../core/services/cart.service';
import { AccountService } from '../../core/services/account.service';
import { ProductDetail, ProductVariant } from '../../core/models/api.models';
import { ProductReviewSummary } from '../../core/models/account.models';
import { StoreCurrencyPipe } from '../../core/pipes/store-currency.pipe';

/**
 * Product detail page. Shows media, description, variant selector, stock, and
 * an add-to-cart control. The selected variant drives the displayed price.
 */
@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StoreCurrencyPipe],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnInit {
  product: ProductDetail | null = null;
  selectedVariant: ProductVariant | null = null;
  quantity = 1;
  added = false;
  loading = false;
  error: string | null = null;

  // Reviews state.
  reviews: ProductReviewSummary | null = null;
  isLoggedIn = false;
  newRating = 5;
  newComment = '';
  reviewSubmitting = false;
  reviewError: string | null = null;
  reviewSuccess = false;

  constructor(
    private catalog: CatalogService,
    private cart: CartService,
    private account: AccountService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.isLoggedIn = this.account.isLoggedIn();
    this.loading = true;
    this.catalog.getProduct(id).subscribe({
      next: p => {
        this.product = p;
        // Default to the first variant when the product has variants.
        this.selectedVariant = p.variants.length ? p.variants[0] : null;
        this.loading = false;
      },
      error: () => {
        this.error = 'Product not found.';
        this.loading = false;
      }
    });
    this.loadReviews(id);
  }

  /** Loads the product's review summary (public). */
  loadReviews(productId: number): void {
    this.account.getProductReviews(productId).subscribe({
      next: summary => (this.reviews = summary),
      error: () => (this.reviews = null)
    });
  }

  /** Average rating rounded to a whole number of stars (0-5). */
  get roundedAverage(): number {
    return this.reviews ? Math.round(this.reviews.averageRating) : 0;
  }

  /** Renders a 1-5 rating as filled/empty stars. */
  stars(rating: number): string {
    const r = Math.max(0, Math.min(5, rating));
    return '★★★★★'.slice(0, r) + '☆☆☆☆☆'.slice(0, 5 - r);
  }

  /** Submits a new review for the current product (server gates to purchasers). */
  submitReview(): void {
    if (!this.product) {
      return;
    }
    this.reviewSubmitting = true;
    this.reviewError = null;
    this.reviewSuccess = false;
    this.account
      .createReview({ productId: this.product.id, rating: this.newRating, comment: this.newComment.trim() || null })
      .subscribe({
        next: () => {
          this.reviewSubmitting = false;
          this.reviewSuccess = true;
          this.newComment = '';
          this.newRating = 5;
          this.loadReviews(this.product!.id);
        },
        error: err => {
          this.reviewSubmitting = false;
          // Surface the server's gating message (not purchased / already reviewed).
          this.reviewError = err?.error?.message ?? 'Could not submit your review.';
        }
      });
  }

  /** Current price: the selected variant's price, else the base price. */
  get currentPrice(): number {
    if (this.selectedVariant) {
      return this.selectedVariant.price;
    }
    return this.product?.basePrice ?? 0;
  }

  /** Whether the product has any stock on hand. */
  get inStock(): boolean {
    return (this.product?.totalQuantityOnHand ?? 0) > 0;
  }

  selectVariant(v: ProductVariant): void {
    this.selectedVariant = v;
  }

  /** Adds the chosen product/variant/quantity to the cart. */
  addToCart(): void {
    if (!this.product) {
      return;
    }
    this.cart.add(this.product.id, this.quantity, this.selectedVariant?.id ?? null);
    this.added = true;
    setTimeout(() => (this.added = false), 2000);
  }

  goToCart(): void {
    this.router.navigate(['/cart']);
  }
}
